using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GrantAnalyticsReaderSelect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Text-to-SQL runs as a role that can only read the business tables. The login itself is
            // created outside migrations (docker/postgres/initdb), because it carries a password.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'analytics_reader') THEN
                        CREATE ROLE analytics_reader NOLOGIN;
                    END IF;
                END
                $$;
                GRANT SELECT ON restaurants, menu_items, orders, order_items, reviews TO analytics_reader;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("REVOKE SELECT ON restaurants, menu_items, orders, order_items, reviews FROM analytics_reader;");
        }
    }
}
