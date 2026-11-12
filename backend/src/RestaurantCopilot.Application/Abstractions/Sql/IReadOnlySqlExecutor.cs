using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Abstractions.Sql;

public interface IReadOnlySqlExecutor
{
    /// <summary>Description of the queryable tables and columns, written for the LLM.</summary>
    string SchemaDescription { get; }

    /// <summary>Runs SQL already approved by <see cref="ISqlQueryGuard"/> with a read-only role and a timeout.</summary>
    Task<Result<SqlQueryResult>> ExecuteAsync(string guardedSql, CancellationToken cancellationToken);
}

/// <param name="Truncated">True when the query returned more rows than the configured maximum.</param>
public sealed record SqlQueryResult(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<object?>> Rows, bool Truncated);
