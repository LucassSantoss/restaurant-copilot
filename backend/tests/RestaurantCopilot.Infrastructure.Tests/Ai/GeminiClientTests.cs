using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Infrastructure.Ai.Gemini;

namespace RestaurantCopilot.Infrastructure.Tests.Ai;

public sealed class GeminiClientTests
{
    private static readonly JsonElement SalesToolSchema = JsonDocument.Parse("""
        { "type": "object", "properties": { "period": { "type": "string" } }, "required": ["period"] }
        """).RootElement;

    private readonly FakeHttpMessageHandler _handler = new();

    [Fact]
    public async Task GenerateAsync_SendsSystemInstructionToolsAndSchemaToConfiguredModel()
    {
        _handler.RespondWith(HttpStatusCode.OK, GeminiTestData.TextResponse("ok"));
        var request = new LlmRequest("Você é um assistente.", [new UserMessage("Quanto vendi?")])
        {
            Tools = [new LlmToolDeclaration("search_sales", "Busca vendas.", SalesToolSchema)],
            ResponseJsonSchema = SalesToolSchema,
            Temperature = 0.2,
        };

        await CreateClient().GenerateAsync(request, TestContext.Current.CancellationToken);

        var (httpRequest, body) = _handler.Requests.ShouldHaveSingleItem();
        httpRequest.RequestUri!.AbsolutePath.ShouldEndWith("/models/test-flash:generateContent");
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        root.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString().ShouldBe("Você é um assistente.");
        root.GetProperty("contents")[0].GetProperty("role").GetString().ShouldBe("user");
        root.GetProperty("tools")[0].GetProperty("functionDeclarations")[0].GetProperty("name").GetString().ShouldBe("search_sales");
        var config = root.GetProperty("generationConfig");
        config.GetProperty("responseMimeType").GetString().ShouldBe("application/json");
        config.GetProperty("responseJsonSchema").GetProperty("required")[0].GetString().ShouldBe("period");
        config.GetProperty("temperature").GetDouble().ShouldBe(0.2);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsAnswerTextWithoutThoughtsAndUsage()
    {
        _handler.RespondWith(HttpStatusCode.OK, """
            {
              "candidates": [{ "content": { "role": "model", "parts": [
                { "text": "pensando...", "thought": true },
                { "text": "Você vendeu " }, { "text": "R$ 100." }
              ] }, "finishReason": "STOP" }],
              "usageMetadata": { "promptTokenCount": 30, "candidatesTokenCount": 8 }
            }
            """);

        var result = await CreateClient().GenerateAsync(SimpleRequest(), TestContext.Current.CancellationToken);

        result.Value.Text.ShouldBe("Você vendeu R$ 100.");
        result.Value.HasToolCalls.ShouldBeFalse();
        result.Value.Usage.ShouldBe(new LlmUsage(30, 8));
    }

    [Fact]
    public async Task GenerateAsync_ParsesFunctionCallsAndReplaysTheirSignature()
    {
        _handler
            .RespondWith(HttpStatusCode.OK, """
                {
                  "candidates": [{ "content": { "role": "model", "parts": [
                    { "functionCall": { "name": "search_sales", "args": { "period": "last_week" }, "id": "call-1" }, "thoughtSignature": "sig-123" }
                  ] }, "finishReason": "STOP" }]
                }
                """)
            .RespondWith(HttpStatusCode.OK, GeminiTestData.TextResponse("R$ 100"));
        var client = CreateClient();

        var first = await client.GenerateAsync(SimpleRequest(), TestContext.Current.CancellationToken);
        var call = first.Value.ToolCalls.ShouldHaveSingleItem();
        await client.GenerateAsync(
            new LlmRequest(null,
            [
                new UserMessage("Quanto vendi?"),
                new AssistantMessage(null, [call]),
                new ToolResultMessage(call.Name, JsonDocument.Parse("123.4").RootElement, call.CallId),
            ]),
            TestContext.Current.CancellationToken);

        call.Name.ShouldBe("search_sales");
        call.Arguments.GetProperty("period").GetString().ShouldBe("last_week");
        call.CallId.ShouldBe("call-1");
        using var replay = JsonDocument.Parse(_handler.Requests[1].Body);
        var contents = replay.RootElement.GetProperty("contents");
        contents[1].GetProperty("role").GetString().ShouldBe("model");
        contents[1].GetProperty("parts")[0].GetProperty("thoughtSignature").GetString().ShouldBe("sig-123");
        var functionResponse = contents[2].GetProperty("parts")[0].GetProperty("functionResponse");
        functionResponse.GetProperty("id").GetString().ShouldBe("call-1");
        functionResponse.GetProperty("response").GetProperty("result").GetDouble().ShouldBe(123.4);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "Llm.RateLimited")]
    [InlineData(HttpStatusCode.Forbidden, "Llm.Unauthorized")]
    [InlineData(HttpStatusCode.BadRequest, "Llm.InvalidRequest")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Llm.Unavailable")]
    public async Task GenerateAsync_MapsHttpErrors(HttpStatusCode statusCode, string expectedCode)
    {
        _handler.RespondWith(statusCode, """{ "error": { "code": 400, "message": "bad", "status": "X" } }""");

        var result = await CreateClient().GenerateAsync(SimpleRequest(), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task GenerateAsync_WhenPromptIsBlocked_ReturnsBlocked()
    {
        _handler.RespondWith(HttpStatusCode.OK, """{ "promptFeedback": { "blockReason": "SAFETY" } }""");

        var result = await CreateClient().GenerateAsync(SimpleRequest(), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(LlmErrors.Blocked);
    }

    [Fact]
    public async Task GenerateAsync_WithoutApiKey_FailsWithoutCallingGemini()
    {
        var options = GeminiTestData.Wrap(new GeminiOptions { ApiKey = "" });
        var client = new GeminiClient(GeminiTestData.CreateHttpClient(_handler), options, NullLogger<GeminiClient>.Instance);

        var result = await client.GenerateAsync(SimpleRequest(), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(LlmErrors.NotConfigured);
        _handler.Requests.ShouldBeEmpty();
    }

    private GeminiClient CreateClient() =>
        new(GeminiTestData.CreateHttpClient(_handler), GeminiTestData.Wrap(GeminiTestData.Options), NullLogger<GeminiClient>.Instance);

    private static LlmRequest SimpleRequest() => new(null, [new UserMessage("Quanto vendi?")]);
}
