using System.Text.Json;
using RestaurantCopilot.Application.Abstractions.Sql;
using RestaurantCopilot.Application.Agent;
using RestaurantCopilot.Application.Agent.Tools;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Tests.Agent;

public sealed class QueryDatabaseToolTests
{
    private static readonly AgentContext Context = new(TestData.Restaurant(), new DateOnly(2026, 11, 11));

    private readonly ISqlQueryGuard _guard = Substitute.For<ISqlQueryGuard>();
    private readonly IReadOnlySqlExecutor _executor = Substitute.For<IReadOnlySqlExecutor>();

    [Fact]
    public async Task ExecuteAsync_RunsOnlyTheGuardedSql()
    {
        _guard.Validate("SELECT 1").Returns(Result.Success("SELECT * FROM (SELECT 1) AS guarded_query LIMIT 101"));
        _executor.ExecuteAsync("SELECT * FROM (SELECT 1) AS guarded_query LIMIT 101", Arg.Any<CancellationToken>())
            .Returns(new SqlQueryResult(["one"], [[1]], Truncated: false));

        var result = await CreateTool().ExecuteAsync(Arguments("SELECT 1"), Context, TestContext.Current.CancellationToken);

        result.Value.Sources.ShouldHaveSingleItem().Description.ShouldBe("Consulta SQL: Teste");
        JsonSerializer.SerializeToElement(result.Value.Data).GetProperty("RowCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGuardRejects_NeverTouchesTheDatabase()
    {
        _guard.Validate(Arg.Any<string>()).Returns(Result.Failure<string>(Error.Validation("Sql.Rejected", "Query rejected: 'DROP' is not allowed.")));

        var result = await CreateTool().ExecuteAsync(Arguments("DROP TABLE orders"), Context, TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("Sql.Rejected");
        await _executor.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Description_IncludesTheSchema()
    {
        _executor.SchemaDescription.Returns("- orders(id, number)");

        CreateTool().Description.ShouldContain("- orders(id, number)");
    }

    private QueryDatabaseTool CreateTool() => new(_guard, _executor);

    private static JsonElement Arguments(string sql) =>
        JsonSerializer.SerializeToElement(new { sql, purpose = "Teste" });
}
