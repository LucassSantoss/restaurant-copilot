using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Abstractions.Ai;

public static class LlmErrors
{
    public static readonly Error NotConfigured = Error.Unavailable(
        "Llm.NotConfigured", "The LLM provider is not configured (missing API key).");

    public static readonly Error Unauthorized = Error.Unavailable(
        "Llm.Unauthorized", "The LLM provider rejected the configured API key.");

    public static readonly Error RateLimited = Error.Unavailable(
        "Llm.RateLimited", "The LLM provider rate limit was reached. Try again in a few moments.");

    public static readonly Error Unavailable = Error.Unavailable(
        "Llm.Unavailable", "The LLM provider is unavailable or timed out.");

    public static readonly Error Blocked = Error.Failure(
        "Llm.Blocked", "The LLM provider refused to answer this request.");

    public static Error InvalidRequest(string detail) => Error.Failure("Llm.InvalidRequest", detail);

    public static Error InvalidResponse(string detail) => Error.Failure("Llm.InvalidResponse", detail);
}
