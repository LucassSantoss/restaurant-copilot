using Microsoft.Extensions.Options;
using RestaurantCopilot.Infrastructure.Sql;

namespace RestaurantCopilot.Infrastructure.Tests.Sql;

public sealed class SqlQueryGuardTests
{
    private readonly SqlQueryGuard _guard = new(Options.Create(new TextToSqlOptions { MaxRows = 100, MaxSqlLength = 4000 }));

    [Theory]
    [InlineData("SELECT count(*) FROM orders")]
    [InlineData("select name, price from menu_items where category = 'Pizza' order by price desc")]
    [InlineData("WITH weekly AS (SELECT date_trunc('week', placed_at AT TIME ZONE 'America/Sao_Paulo')::date AS week FROM orders) SELECT week, count(*) FROM weekly GROUP BY week")]
    [InlineData("SELECT count(*) FILTER (WHERE rating <= 2) FROM reviews WHERE 'Packaging' = ANY(themes)")]
    [InlineData("SELECT comment, created_at FROM reviews WHERE created_at > now() - interval '30 days'")]
    [InlineData("SELECT \"update\" FROM (SELECT 1 AS \"update\") AS t")]
    [InlineData("SELECT 'DROP TABLE orders; -- not a command, just text' AS label")]
    public void Validate_AcceptsReadOnlyQueries(string sql)
    {
        _guard.Validate(sql).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WrapsQueryWithRowLimit()
    {
        var result = _guard.Validate("SELECT name FROM menu_items LIMIT 100000");

        result.Value.ShouldBe("SELECT * FROM (SELECT name FROM menu_items LIMIT 100000) AS guarded_query LIMIT 101");
    }

    [Theory]
    [InlineData("INSERT INTO orders (number) VALUES (1)", "INSERT")]
    [InlineData("UPDATE menu_items SET price = 0", "UPDATE")]
    [InlineData("DELETE FROM reviews", "DELETE")]
    [InlineData("DROP TABLE orders", "DROP")]
    [InlineData("ALTER TABLE orders ADD COLUMN x int", "ALTER")]
    [InlineData("TRUNCATE orders", "TRUNCATE")]
    [InlineData("SELECT * INTO stolen FROM orders", "INTO")]
    [InlineData("SELECT * FROM orders FOR UPDATE", "UPDATE")]
    [InlineData("WITH gone AS (DELETE FROM orders RETURNING *) SELECT * FROM gone", "DELETE")]
    [InlineData("sElEcT 1; DrOp TaBlE orders", "DROP")]
    [InlineData("COPY orders TO '/tmp/orders.csv'", "COPY")]
    [InlineData("SET statement_timeout = 0", "SET")]
    public void Validate_RejectsDataChangingStatements(string sql, string keyword)
    {
        var result = _guard.Validate(sql);

        result.Error.Code.ShouldBe("Sql.Rejected");
        result.Error.Message.ShouldContain(keyword);
    }

    [Theory]
    [InlineData("SELECT 1; SELECT 2")]
    [InlineData("SELECT 1;;SELECT 2")]
    public void Validate_RejectsMultipleStatements(string sql)
    {
        _guard.Validate(sql).Error.Message.ShouldContain("exactly one statement");
    }

    [Theory]
    [InlineData("SELECT 1 -- harmless?")]
    [InlineData("SELECT /* hidden */ 1")]
    [InlineData("SELECT 1 /* ; DROP TABLE orders */")]
    public void Validate_RejectsComments(string sql)
    {
        _guard.Validate(sql).Error.Message.ShouldContain("comments");
    }

    [Theory]
    [InlineData("SELECT usename, passwd FROM pg_shadow")]
    [InlineData("SELECT * FROM pg_catalog.pg_roles")]
    [InlineData("SELECT table_name FROM information_schema.tables")]
    [InlineData("SELECT pg_read_file('/etc/passwd')")]
    [InlineData("SELECT pg_sleep(60)")]
    [InlineData("SELECT set_config('statement_timeout', '0', false)")]
    [InlineData("SELECT * FROM search_documents")]
    [InlineData("SELECT * FROM \"__EFMigrationsHistory\"")]
    public void Validate_RejectsSystemCatalogsAndInternalTables(string sql)
    {
        _guard.Validate(sql).Error.Message.ShouldContain("not accessible");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SELEC * FROM orders")]
    [InlineData("SELECT * FROM")]
    public void Validate_RejectsEmptyOrInvalidSql(string sql)
    {
        _guard.Validate(sql).Error.Code.ShouldBe("Sql.Rejected");
    }

    [Fact]
    public void Validate_RejectsOverlongSql()
    {
        var sql = "SELECT " + string.Join(", ", Enumerable.Repeat("1", 3000));

        _guard.Validate(sql).Error.Message.ShouldContain("longer than");
    }
}
