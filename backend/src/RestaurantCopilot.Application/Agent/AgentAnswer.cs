using System.Text.Json;
using RestaurantCopilot.Application.Abstractions.Ai;

namespace RestaurantCopilot.Application.Agent;

/// <param name="Sources">What grounded the answer: the tools whose data was used and the retrieved documents.</param>
/// <param name="Steps">Every tool call the model made, in order, for transparency and debugging.</param>
public sealed record AgentAnswer(
    string Answer,
    IReadOnlyList<AgentSource> Sources,
    IReadOnlyList<AgentStep> Steps,
    LlmUsage Usage);

/// <param name="Kind">"tool" for data returned by a tool, "document" for a retrieved menu item or review.</param>
/// <param name="Reference">Tool name, or the document id.</param>
public sealed record AgentSource(string Kind, string Reference, string Description)
{
    public static AgentSource Tool(string name, string description) => new("tool", name, description);

    public static AgentSource Document(Guid id, string excerpt) => new("document", id.ToString(), excerpt);
}

public sealed record AgentStep(string Tool, JsonElement Arguments, bool Succeeded, string? Error);
