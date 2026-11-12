using System.ComponentModel;
using RestaurantCopilot.Application.Abstractions.Sql;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Agent.Tools;

/// <summary>
/// Text-to-SQL: the model writes a SELECT for analytical questions the other tools do not cover.
/// The SQL is validated by the guard and runs with a read-only role; rejections and database errors
/// go back to the model so it can fix the query.
/// </summary>
internal sealed class QueryDatabaseTool(ISqlQueryGuard guard, IReadOnlySqlExecutor executor)
    : AgentTool<QueryDatabaseTool.Arguments>
{
    public override string Name => "query_database";

    public override string Description =>
        "Runs one read-only PostgreSQL SELECT for analytical questions the other tools cannot answer "
        + "(custom aggregations, rankings, hours of the day, combinations of filters). Prefer the specialized tools "
        + "when they fit. Only a single SELECT is accepted: no comments, no data changes, results are capped.\n"
        + executor.SchemaDescription;

    protected override async Task<Result<AgentToolResult>> ExecuteAsync(
        Arguments arguments,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var guarded = guard.Validate(arguments.Sql);
        if (guarded.IsFailure)
        {
            return guarded.Error;
        }

        var result = await executor.ExecuteAsync(guarded.Value, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var data = new { result.Value.Columns, result.Value.Rows, RowCount = result.Value.Rows.Count, result.Value.Truncated };
        return new AgentToolResult(data, [AgentSource.Tool(Name, $"Consulta SQL: {arguments.Purpose}")]);
    }

    internal sealed record Arguments(
        [property: Description("A single PostgreSQL SELECT statement.")] string Sql,
        [property: Description("One short sentence, in Portuguese, saying what the query computes.")] string Purpose);
}
