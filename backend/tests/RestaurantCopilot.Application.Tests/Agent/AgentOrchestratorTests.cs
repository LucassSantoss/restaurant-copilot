using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Agent;
using RestaurantCopilot.Application.Agent.Tools;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Tests.Agent;

public sealed class AgentOrchestratorTests
{
    private readonly IAgentTool _salesTool = Substitute.For<IAgentTool>();
    private readonly IRestaurantRepository _restaurants = Substitute.For<IRestaurantRepository>();
    private readonly IMenuItemRepository _menuItems = Substitute.For<IMenuItemRepository>();
    private readonly ISalesRepository _sales = Substitute.For<ISalesRepository>();

    public AgentOrchestratorTests()
    {
        _restaurants.GetAsync(Arg.Any<CancellationToken>()).Returns(TestData.Restaurant());
        _menuItems.ListAsync(null, false, Arg.Any<CancellationToken>()).Returns([]);
        _salesTool.Name.Returns("search_sales");
        _salesTool.Description.Returns("Sales.");
        _salesTool.ParametersSchema.Returns(JsonDocument.Parse("""{ "type": "object" }""").RootElement);
        _salesTool.ExecuteAsync(Arg.Any<JsonElement>(), Arg.Any<AgentContext>(), Arg.Any<CancellationToken>())
            .Returns(new AgentToolResult(new { revenue = 1234.5m }, [AgentSource.Tool("search_sales", "Vendas de outubro")]));
    }

    [Fact]
    public async Task AnswerAsync_WithoutToolCalls_ReturnsTheModelAnswer()
    {
        var llm = new ScriptedLlmClient(ScriptedLlmClient.Answer("Olá!"));

        var result = await CreateOrchestrator(llm).AnswerAsync("Oi", TestContext.Current.CancellationToken);

        result.Value.Answer.ShouldBe("Olá!");
        result.Value.Steps.ShouldBeEmpty();
        var request = llm.Requests.ShouldHaveSingleItem();
        request.Tools.ShouldHaveSingleItem().Name.ShouldBe("search_sales");
        request.SystemInstruction.ShouldNotBeNull();
        request.SystemInstruction.ShouldContain("Today is 2026-11-11");
    }

    [Fact]
    public async Task AnswerAsync_RunsRequestedToolAndFeedsResultBack()
    {
        var llm = new ScriptedLlmClient(
            ScriptedLlmClient.CallTool("search_sales", """{ "period_start": "2026-10-01", "period_end": "2026-10-31" }"""),
            ScriptedLlmClient.Answer("Você faturou R$ 1.234,50 em outubro."));

        var result = await CreateOrchestrator(llm).AnswerAsync("Quanto faturei em outubro?", TestContext.Current.CancellationToken);

        result.Value.Answer.ShouldBe("Você faturou R$ 1.234,50 em outubro.");
        result.Value.Sources.ShouldBe([AgentSource.Tool("search_sales", "Vendas de outubro")]);
        result.Value.Steps.ShouldHaveSingleItem().Succeeded.ShouldBeTrue();
        result.Value.Usage.ShouldBe(new LlmUsage(180, 30));

        var followUp = llm.Requests[1].Messages;
        followUp[1].ShouldBeOfType<AssistantMessage>().ToolCalls.ShouldHaveSingleItem().Name.ShouldBe("search_sales");
        var toolResult = followUp[2].ShouldBeOfType<ToolResultMessage>();
        toolResult.CallId.ShouldBe("call-search_sales");
        toolResult.Result.GetProperty("revenue").GetDecimal().ShouldBe(1234.5m);
    }

    [Fact]
    public async Task AnswerAsync_WhenModelCallsUnknownTool_ReportsErrorToModelAndContinues()
    {
        var llm = new ScriptedLlmClient(
            ScriptedLlmClient.CallTool("delete_everything", "{}"),
            ScriptedLlmClient.Answer("Não consigo fazer isso."));

        var result = await CreateOrchestrator(llm).AnswerAsync("Apague tudo", TestContext.Current.CancellationToken);

        result.Value.Answer.ShouldBe("Não consigo fazer isso.");
        var step = result.Value.Steps.ShouldHaveSingleItem();
        step.Succeeded.ShouldBeFalse();
        step.Error.ShouldNotBeNull();
        step.Error.ShouldContain("search_sales");
        llm.Requests[1].Messages[2].ShouldBeOfType<ToolResultMessage>().Result.GetProperty("error").GetString().ShouldContain("delete_everything");
    }

    [Fact]
    public async Task AnswerAsync_WhenToolFails_SendsErrorMessageToModel()
    {
        _salesTool.ExecuteAsync(Arg.Any<JsonElement>(), Arg.Any<AgentContext>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AgentToolResult>(AgentToolErrors.InvalidArguments("period_end must be on or after period_start.")));
        var llm = new ScriptedLlmClient(ScriptedLlmClient.CallTool("search_sales", "{}"), ScriptedLlmClient.Answer("Corrigido."));

        var result = await CreateOrchestrator(llm).AnswerAsync("Vendas?", TestContext.Current.CancellationToken);

        result.Value.Sources.ShouldBeEmpty();
        llm.Requests[1].Messages[2].ShouldBeOfType<ToolResultMessage>().Result.GetProperty("error").GetString()
            .ShouldBe("period_end must be on or after period_start.");
    }

    [Fact]
    public async Task AnswerAsync_AfterMaxIterations_ForcesAnswerWithoutTools()
    {
        var llm = new ScriptedLlmClient(
            ScriptedLlmClient.CallTool("search_sales", "{}"),
            ScriptedLlmClient.CallTool("search_sales", "{}"),
            request => request.Tools.Count == 0
                ? new LlmResponse("Resposta parcial.", [], LlmUsage.None)
                : throw new InvalidOperationException("Tools should be disabled on the final turn."));

        var result = await CreateOrchestrator(llm, maxIterations: 2).AnswerAsync("Vendas?", TestContext.Current.CancellationToken);

        result.Value.Answer.ShouldBe("Resposta parcial.");
        llm.Requests.Count.ShouldBe(3);
        llm.Requests[2].Messages[^1].ShouldBeOfType<UserMessage>().Text.ShouldBe(AgentPrompt.FinalAnswerInstruction);
    }

    [Fact]
    public async Task AnswerAsync_PropagatesLlmErrors()
    {
        var llm = new ScriptedLlmClient(_ => LlmErrors.RateLimited);

        var result = await CreateOrchestrator(llm).AnswerAsync("Vendas?", TestContext.Current.CancellationToken);

        result.Error.ShouldBe(LlmErrors.RateLimited);
    }

    [Fact]
    public async Task AnswerAsync_WhenTimeoutElapses_ReturnsTimeoutError()
    {
        var llm = Substitute.For<ILlmClient>();
        llm.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
                return Result.Success(new LlmResponse("never", [], LlmUsage.None));
            });

        var result = await CreateOrchestrator(llm, timeout: TimeSpan.FromMilliseconds(50)).AnswerAsync("Vendas?", TestContext.Current.CancellationToken);

        result.Error.ShouldBe(AgentErrors.Timeout);
    }

    private AgentOrchestrator CreateOrchestrator(ILlmClient llm, int maxIterations = 6, TimeSpan? timeout = null) =>
        new(
            llm,
            [_salesTool],
            _restaurants,
            _menuItems,
            _sales,
            new FixedTimeProvider(TestData.Now),
            Options.Create(new AgentOptions { MaxIterations = maxIterations, Timeout = timeout ?? TimeSpan.FromSeconds(30) }),
            NullLogger<AgentOrchestrator>.Instance);
}
