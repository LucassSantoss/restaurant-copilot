using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using RestaurantCopilot.Application.Abstractions.Sql;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Infrastructure.Sql;

/// <summary>
/// Runs guarded SQL through a dedicated data source that logs in as a read-only role, inside a
/// READ ONLY transaction with a statement timeout: even SQL that slipped past the guard cannot
/// change data or run for long.
/// </summary>
internal sealed class ReadOnlySqlExecutor(
    ReadOnlyDataSource dataSource,
    IOptions<TextToSqlOptions> options,
    ILogger<ReadOnlySqlExecutor> logger) : IReadOnlySqlExecutor
{
    /// <summary>PostgreSQL error code raised when statement_timeout cancels a query.</summary>
    private const string QueryCanceledSqlState = "57014";

    private readonly TextToSqlOptions _options = options.Value;

    public string SchemaDescription => """
        Tables (PostgreSQL, read-only). Timestamps are timestamptz stored in UTC: convert with
        AT TIME ZONE '<restaurant time zone>' before taking dates, hours or weekdays (EXTRACT(ISODOW ...): 1 = Monday).
        - restaurants(id uuid, name, cuisine, city, time_zone_id)
        - menu_items(id uuid, name, description, category text [Starter|Pizza|Pasta|Risotto|MainCourse|Dessert|Drink], price numeric, is_available bool)
        - orders(id uuid, number int, placed_at timestamptz, channel text [Delivery|Pickup|DineIn], status text [Completed|Cancelled])
        - order_items(id uuid, order_id -> orders.id, menu_item_id -> menu_items.id, quantity int, unit_price numeric)
          Revenue = SUM(quantity * unit_price) over orders with status = 'Completed'.
        - reviews(id uuid, order_id -> orders.id, menu_item_id -> menu_items.id (nullable), rating int 1-5, comment text,
          created_at timestamptz, sentiment text [Positive|Neutral|Negative] (null until classified),
          themes text[] of [Delay|Taste|Packaging|Price|Service] (filter with 'Packaging' = ANY(themes)), classified_at timestamptz)
        """;

    public async Task<Result<SqlQueryResult>> ExecuteAsync(string guardedSql, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.Value.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await using (var setup = new NpgsqlCommand(
                string.Create(CultureInfo.InvariantCulture, $"SET TRANSACTION READ ONLY; SET LOCAL statement_timeout = {(int)_options.StatementTimeout.TotalMilliseconds}"),
                connection,
                transaction))
            {
                await setup.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var command = new NpgsqlCommand(guardedSql, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            var rows = new List<IReadOnlyList<object?>>();
            while (await reader.ReadAsync(cancellationToken) && rows.Count <= _options.MaxRows)
            {
                rows.Add(Enumerable.Range(0, reader.FieldCount).Select(index => reader.IsDBNull(index) ? null : reader.GetValue(index)).ToList());
            }

            var truncated = rows.Count > _options.MaxRows;
            return new SqlQueryResult(columns, truncated ? rows[.._options.MaxRows] : rows, truncated);
        }
        catch (PostgresException exception) when (exception.SqlState == QueryCanceledSqlState)
        {
            logger.LogWarning("Text-to-SQL query timed out");
            return SqlErrors.Timeout;
        }
        catch (PostgresException exception)
        {
            logger.LogInformation("Text-to-SQL query failed: {Message}", exception.MessageText);
            return SqlErrors.ExecutionFailed(exception.MessageText);
        }
    }
}

/// <summary>
/// The read-only data source, wrapped so it cannot be confused with the main (read-write) one in DI.
/// </summary>
internal sealed class ReadOnlyDataSource(NpgsqlDataSource value) : IAsyncDisposable
{
    public NpgsqlDataSource Value { get; } = value;

    public ValueTask DisposeAsync() => Value.DisposeAsync();
}
