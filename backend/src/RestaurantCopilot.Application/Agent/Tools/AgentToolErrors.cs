using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Agent.Tools;

/// <summary>Tool failures; their messages are sent back to the model so it can correct itself.</summary>
public static class AgentToolErrors
{
    public static Error InvalidArguments(string detail) => Error.Validation("AgentTool.InvalidArguments", detail);

    public static Error UnknownTool(string name, IEnumerable<string> available) => Error.NotFound(
        "AgentTool.Unknown", $"There is no tool named '{name}'. Available tools: {string.Join(", ", available)}.");

    public static Error UnknownMenuItem(string name, IEnumerable<string> available) => Error.NotFound(
        "AgentTool.UnknownMenuItem", $"No dish matches '{name}'. Menu: {string.Join("; ", available)}.");
}
