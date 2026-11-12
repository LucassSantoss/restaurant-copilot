namespace RestaurantCopilot.Application.Agent;

/// <summary>Safety limits of the agent loop, bound from the "Agent" configuration section.</summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>Maximum model turns that may request tools before the agent must answer.</summary>
    public int MaxIterations { get; init; } = 6;

    /// <summary>Upper bound for answering one question, tool calls and retries included.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(120);
}
