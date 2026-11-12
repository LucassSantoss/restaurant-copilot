using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Infrastructure.Sql;

internal static class SqlErrors
{
    public static Error Rejected(string reason) => Error.Validation("Sql.Rejected", $"Query rejected: {reason}");

    public static Error ExecutionFailed(string detail) => Error.Validation("Sql.ExecutionFailed", $"The database rejected the query: {detail}");

    public static readonly Error Timeout = Error.Validation(
        "Sql.Timeout", "The query exceeded the time limit. Simplify it or filter a shorter period.");
}
