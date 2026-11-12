using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RestaurantCopilot.Api.Tests.Infrastructure;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Api.Tests.Chat;

[Collection(ApiTestCollectionDefinition.Name)]
public sealed class ChatEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Post_RunsToolsAgainstRealDataAndReturnsSources()
    {
        // The model asks for October sales, then answers with the figure the tool returned.
        JsonElement? toolResult = null;
        var llm = Substitute.For<ILlmClient>();
        llm.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>()).Returns(
            _ => Result.Success(new LlmResponse(
                null,
                [new LlmToolCall("search_sales", JsonDocument.Parse("""{ "period_start": "2026-10-01", "period_end": "2026-10-31" }""").RootElement)],
                new LlmUsage(500, 20))),
            call =>
            {
                toolResult = call.Arg<LlmRequest>().Messages.OfType<ToolResultMessage>().Single().Result;
                return Result.Success(new LlmResponse("Em outubro você faturou bem.", [], new LlmUsage(800, 40)));
            });
        await using var app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton(llm)));
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/chat", new { question = "Quanto faturei em outubro?" }, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ChatBody>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Answer.ShouldBe("Em outubro você faturou bem.");
        body.Sources.ShouldHaveSingleItem().Reference.ShouldBe("search_sales");
        body.ToolCalls.ShouldHaveSingleItem().Succeeded.ShouldBeTrue();
        body.Usage.InputTokens.ShouldBe(1300);
        toolResult.ShouldNotBeNull();
        toolResult.Value.GetProperty("revenue").GetDecimal().ShouldBeGreaterThan(0m);
        toolResult.Value.GetProperty("completed_orders").GetInt32().ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData("SELECT count(*) AS orders FROM orders WHERE status = 'Completed'", true)]
    [InlineData("DELETE FROM orders", false)]
    public async Task Post_TextToSqlRunsOnlyGuardedReadOnlyQueries(string sql, bool shouldSucceed)
    {
        JsonElement? toolResult = null;
        var llm = Substitute.For<ILlmClient>();
        llm.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>()).Returns(
            _ => Result.Success(new LlmResponse(
                null,
                [new LlmToolCall("query_database", JsonSerializer.SerializeToElement(new { sql, purpose = "Contar pedidos" }))],
                LlmUsage.None)),
            call =>
            {
                toolResult = call.Arg<LlmRequest>().Messages.OfType<ToolResultMessage>().Single().Result;
                return Result.Success(new LlmResponse("Pronto.", [], LlmUsage.None));
            });
        await using var app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton(llm)));
        using var client = app.CreateClient();

        // A distinct question per case: the cache would otherwise answer the second case from the first.
        var response = await client.PostAsJsonAsync("/api/chat", new { question = $"Quantos pedidos tive? ({sql})" }, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ChatBody>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.ToolCalls.ShouldHaveSingleItem().Succeeded.ShouldBe(shouldSucceed);
        toolResult.ShouldNotBeNull();
        if (shouldSucceed)
        {
            toolResult.Value.GetProperty("rows")[0][0].GetInt64().ShouldBeGreaterThan(1000);
        }
        else
        {
            toolResult.Value.GetProperty("error").GetString().ShouldNotBeNull().ShouldContain("DELETE");
        }
    }

    [Fact]
    public async Task Post_SameQuestionTwice_IsAnsweredFromCacheUntilDataChanges()
    {
        var llm = Substitute.For<ILlmClient>();
        llm.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new LlmResponse("Seu prato mais vendido é a Pizza Margherita.", [], LlmUsage.None)));
        await using var app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton(llm)));
        using var client = app.CreateClient();
        var question = new { question = $"Qual é meu prato mais vendido? ({Guid.NewGuid()})" };
        var cancellationToken = TestContext.Current.CancellationToken;

        var first = await (await client.PostAsJsonAsync("/api/chat", question, cancellationToken)).Content.ReadFromJsonAsync<ChatBody>(cancellationToken);
        var second = await (await client.PostAsJsonAsync("/api/chat", question, cancellationToken)).Content.ReadFromJsonAsync<ChatBody>(cancellationToken);
        await client.PostAsJsonAsync("/api/reviews", new { orderNumber = 1003, rating = 5, comment = "Ótimo!" }, cancellationToken);
        var afterNewReview = await (await client.PostAsJsonAsync("/api/chat", question, cancellationToken)).Content.ReadFromJsonAsync<ChatBody>(cancellationToken);

        first!.Cached.ShouldBeFalse();
        second!.Cached.ShouldBeTrue();
        second.Answer.ShouldBe(first.Answer);
        afterNewReview!.Cached.ShouldBeFalse();
    }

    [Fact]
    public async Task Post_WithEmptyQuestion_ReturnsValidationProblem()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/chat", new { question = " " }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithoutLlmKey_ReturnsServiceUnavailable()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/chat", new { question = "Quanto vendi?" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    private sealed record ChatBody(string Answer, List<SourceBody> Sources, List<StepBody> ToolCalls, UsageBody Usage, bool Cached);

    private sealed record SourceBody(string Kind, string Reference, string Description);

    private sealed record StepBody(string Tool, bool Succeeded);

    private sealed record UsageBody(int InputTokens, int OutputTokens);
}
