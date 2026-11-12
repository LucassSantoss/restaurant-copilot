using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Abstractions.Sql;
using RestaurantCopilot.Domain.Common;
using SqlParser;
using SqlParser.Ast;
using SqlParser.Dialects;
using SqlParser.Tokens;

namespace RestaurantCopilot.Infrastructure.Sql;

/// <summary>
/// Validates LLM-written SQL with a real PostgreSQL parser and tokenizer instead of regular
/// expressions, which are easy to fool with casing, quoting or comments. Rules:
/// <list type="number">
/// <item>no comments (they can hide content and are never needed);</item>
/// <item>exactly one statement, and it must be a query (SELECT / WITH ... SELECT);</item>
/// <item>no data-changing or session keywords anywhere, so DML hidden in a CTE or
/// <c>SELECT ... INTO</c> / <c>FOR UPDATE</c> are rejected too;</item>
/// <item>no system catalogs or internal tables (<c>pg_*</c>, <c>information_schema</c>, ...);</item>
/// <item>the result is wrapped in an outer LIMIT, whatever the query asks for.</item>
/// </list>
/// </summary>
internal sealed class SqlQueryGuard(IOptions<TextToSqlOptions> options) : ISqlQueryGuard
{
    private static readonly PostgreSqlDialect Dialect = new();

    private static readonly HashSet<string> ForbiddenKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT", "UPDATE", "DELETE", "MERGE", "UPSERT", "TRUNCATE", "DROP", "ALTER", "CREATE", "RENAME",
        "GRANT", "REVOKE", "COPY", "CALL", "DO", "EXECUTE", "PREPARE", "DEALLOCATE", "INTO", "LOCK",
        "VACUUM", "ANALYZE", "REINDEX", "CLUSTER", "REFRESH", "SET", "RESET", "LISTEN", "NOTIFY",
        "BEGIN", "COMMIT", "ROLLBACK", "SAVEPOINT", "LOAD", "IMPORT", "SECURITY",
    };

    private static readonly HashSet<string> ForbiddenIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "information_schema", "search_documents", "__EFMigrationsHistory", "dblink", "current_setting", "set_config",
    };

    private readonly TextToSqlOptions _options = options.Value;

    public Result<string> Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return SqlErrors.Rejected("the query is empty.");
        }

        if (sql.Length > _options.MaxSqlLength)
        {
            return SqlErrors.Rejected($"the query is longer than {_options.MaxSqlLength} characters.");
        }

        IList<Token> tokens;
        Sequence<Statement> statements;
        try
        {
            tokens = new Tokenizer().Tokenize(sql, Dialect);
            statements = new Parser().ParseSql(sql, Dialect);
        }
        catch (ParserException exception)
        {
            return SqlErrors.Rejected($"it is not valid PostgreSQL ({exception.Message}).");
        }
        catch (TokenizeException exception)
        {
            return SqlErrors.Rejected($"it is not valid PostgreSQL ({exception.Message}).");
        }

        if (CheckTokens(tokens) is { } tokenError)
        {
            return tokenError;
        }

        if (statements.Count != 1)
        {
            return SqlErrors.Rejected("exactly one statement is allowed.");
        }

        if (statements[0] is not Statement.Select select)
        {
            return SqlErrors.Rejected("only SELECT queries are allowed.");
        }

        // One extra row lets the executor tell "exactly MaxRows" from "truncated".
        return $"SELECT * FROM ({select.ToSql()}) AS guarded_query LIMIT {_options.MaxRows + 1}";
    }

    private static Error? CheckTokens(IEnumerable<Token> tokens)
    {
        foreach (var token in tokens)
        {
            switch (token)
            {
                case Whitespace { WhitespaceKind: WhitespaceKind.InlineComment or WhitespaceKind.MultilineComment }:
                    return SqlErrors.Rejected("comments are not allowed.");

                // Quoted identifiers ("update") are names, not keywords; only bare words are commands.
                case Word { QuoteStyle: null } word when ForbiddenKeywords.Contains(word.Value):
                    return SqlErrors.Rejected($"'{word.Value.ToUpperInvariant()}' is not allowed in a read-only query.");

                case Word word when IsForbiddenIdentifier(word.Value):
                    return SqlErrors.Rejected($"'{word.Value}' is not accessible; query only the business tables.");
            }
        }

        return null;
    }

    private static bool IsForbiddenIdentifier(string name) =>
        ForbiddenIdentifiers.Contains(name)
        || name.StartsWith("pg_", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("lo_", StringComparison.OrdinalIgnoreCase);
}
