using System.Text.Json;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Agent.Tools;

/// <summary>
/// A function the agent's LLM can decide to call. The name, description and JSON Schema are sent
/// to the model; the model replies with arguments, which the tool validates and executes.
/// </summary>
public interface IAgentTool
{
    string Name { get; }

    string Description { get; }

    JsonElement ParametersSchema { get; }

    Task<Result<AgentToolResult>> ExecuteAsync(JsonElement arguments, AgentContext context, CancellationToken cancellationToken);
}

/// <param name="Data">Anything JSON-serializable; it is sent back to the model as the tool response.</param>
/// <param name="Sources">What this result should be credited as in the final answer.</param>
public sealed record AgentToolResult(object Data, IReadOnlyList<AgentSource> Sources);
