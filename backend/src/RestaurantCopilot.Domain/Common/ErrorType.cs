namespace RestaurantCopilot.Domain.Common;

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,

    /// <summary>A dependency (e.g. the LLM provider) is down, rate limited or not configured.</summary>
    Unavailable,
}
