using System.Text.Json;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Agent.Tools;

/// <summary>
/// Base for tools with typed arguments: the JSON Schema shown to the model is generated from
/// <typeparamref name="TArguments"/>, and the model's arguments are parsed back into it, so the
/// declaration and the implementation can never disagree.
/// </summary>
internal abstract class AgentTool<TArguments> : IAgentTool
    where TArguments : class
{
    private const int MaxPeriodDays = 366;

    private static readonly JsonElement Schema = LlmClientExtensions.JsonSchemaFor<TArguments>(AgentJson.SerializerOptions);

    public abstract string Name { get; }

    public abstract string Description { get; }

    public JsonElement ParametersSchema => Schema;

    public async Task<Result<AgentToolResult>> ExecuteAsync(
        JsonElement arguments,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        TArguments? parsed;
        try
        {
            parsed = arguments.Deserialize<TArguments>(AgentJson.SerializerOptions);
        }
        catch (JsonException exception)
        {
            return AgentToolErrors.InvalidArguments(exception.Message);
        }

        return parsed is null
            ? AgentToolErrors.InvalidArguments("Arguments must be a JSON object.")
            : await ExecuteAsync(parsed, context, cancellationToken);
    }

    protected abstract Task<Result<AgentToolResult>> ExecuteAsync(
        TArguments arguments,
        AgentContext context,
        CancellationToken cancellationToken);

    protected static Result<ReportingPeriod> Period(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            return AgentToolErrors.InvalidArguments("period_end must be on or after period_start.");
        }

        return end.DayNumber - start.DayNumber >= MaxPeriodDays
            ? AgentToolErrors.InvalidArguments($"The period cannot exceed {MaxPeriodDays} days.")
            : new ReportingPeriod(start, end);
    }
}
