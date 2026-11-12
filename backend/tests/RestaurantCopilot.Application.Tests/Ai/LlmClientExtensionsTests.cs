using System.Text.Json;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Tests.Ai;

public sealed class LlmClientExtensionsTests
{
    private readonly ILlmClient _client = Substitute.For<ILlmClient>();

    [Fact]
    public async Task GenerateJsonAsync_SendsSchemaOfTargetTypeAndDeserializesAnswer()
    {
        LlmRequest? sent = null;
        _client.GenerateAsync(Arg.Do<LlmRequest>(request => sent = request), Arg.Any<CancellationToken>())
            .Returns(Respond("""{ "mood": "Happy", "score": 0.9 }"""));

        var result = await _client.GenerateJsonAsync<Sample>("system", "prompt", TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new Sample(Mood.Happy, 0.9));
        sent.ShouldNotBeNull();
        sent.Temperature.ShouldBe(0);
        var schema = sent.ResponseJsonSchema!.Value;
        schema.GetProperty("properties").GetProperty("mood").GetProperty("enum").EnumerateArray()
            .Select(value => value.GetString()).ShouldBe(["Happy", "Sad"]);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("null")]
    public async Task GenerateJsonAsync_WithUnusableAnswer_ReturnsInvalidResponse(string text)
    {
        _client.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>()).Returns(Respond(text));

        var result = await _client.GenerateJsonAsync<Sample>("system", "prompt", TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("Llm.InvalidResponse");
    }

    [Fact]
    public async Task GenerateJsonAsync_PropagatesProviderErrors()
    {
        _client.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<LlmResponse>(LlmErrors.RateLimited));

        var result = await _client.GenerateJsonAsync<Sample>("system", "prompt", TestContext.Current.CancellationToken);

        result.Error.ShouldBe(LlmErrors.RateLimited);
    }

    private static Result<LlmResponse> Respond(string text) => new LlmResponse(text, [], LlmUsage.None);

    public enum Mood
    {
        Happy,
        Sad,
    }

    public sealed record Sample(Mood Mood, double Score);
}
