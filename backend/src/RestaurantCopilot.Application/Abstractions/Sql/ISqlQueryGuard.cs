using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Abstractions.Sql;

/// <summary>
/// Validates SQL written by the LLM before it reaches the database: only a single read-only SELECT
/// over the business tables is accepted. This is one of three layers, together with a read-only
/// database role and a statement timeout (see <see cref="IReadOnlySqlExecutor"/>).
/// </summary>
public interface ISqlQueryGuard
{
    /// <summary>Returns the SQL to execute (normalized, with a row limit applied) or why it was rejected.</summary>
    Result<string> Validate(string sql);
}
