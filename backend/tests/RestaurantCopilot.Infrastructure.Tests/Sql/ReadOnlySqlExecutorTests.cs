using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using RestaurantCopilot.Infrastructure.Sql;
using RestaurantCopilot.Infrastructure.Tests.Persistence;

namespace RestaurantCopilot.Infrastructure.Tests.Sql;

/// <summary>
/// Runs SQL straight through the executor, bypassing the guard on purpose: the database role and
/// the transaction settings must hold on their own (defense in depth).
/// </summary>
[Collection(PostgresTestCollectionDefinition.Name)]
public sealed class ReadOnlySqlExecutorTests(PostgresFixture fixture) : IAsyncDisposable
{
    private readonly ReadOnlyDataSource _dataSource = new(NpgsqlDataSource.Create(fixture.ReadOnlyConnectionString));

    [Fact]
    public async Task ExecuteAsync_ReturnsColumnsAndRows()
    {
        var result = await CreateExecutor().ExecuteAsync(
            "SELECT category, count(*) AS dishes FROM menu_items GROUP BY category ORDER BY category",
            TestContext.Current.CancellationToken);

        result.Value.Columns.ShouldBe(["category", "dishes"]);
        result.Value.Rows.Count.ShouldBe(7);
        result.Value.Rows[0].ShouldBe(["Dessert", 3L]);
        result.Value.Truncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_TruncatesBeyondMaxRows()
    {
        var result = await CreateExecutor(maxRows: 5).ExecuteAsync("SELECT number FROM orders", TestContext.Current.CancellationToken);

        result.Value.Rows.Count.ShouldBe(5);
        result.Value.Truncated.ShouldBeTrue();
    }

    [Theory]
    [InlineData("DELETE FROM orders")]
    [InlineData("UPDATE menu_items SET price = 0")]
    [InlineData("CREATE TABLE hacked (id int)")]
    public async Task ExecuteAsync_CannotChangeDataEvenWithoutTheGuard(string sql)
    {
        var result = await CreateExecutor().ExecuteAsync(sql, TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("Sql.ExecutionFailed");
    }

    [Fact]
    public async Task ExecuteAsync_CannotReadTablesOutsideTheGrant()
    {
        var result = await CreateExecutor().ExecuteAsync("SELECT count(*) FROM search_documents", TestContext.Current.CancellationToken);

        result.Error.Message.ShouldContain("permission denied");
    }

    [Fact]
    public async Task ExecuteAsync_StopsLongQueriesAtTheTimeout()
    {
        var result = await CreateExecutor(timeout: TimeSpan.FromSeconds(1)).ExecuteAsync("SELECT pg_sleep(5)", TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("Sql.Timeout");
    }

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();

    private ReadOnlySqlExecutor CreateExecutor(int maxRows = 100, TimeSpan? timeout = null) =>
        new(
            _dataSource,
            Options.Create(new TextToSqlOptions { MaxRows = maxRows, StatementTimeout = timeout ?? TimeSpan.FromSeconds(5) }),
            NullLogger<ReadOnlySqlExecutor>.Instance);
}
