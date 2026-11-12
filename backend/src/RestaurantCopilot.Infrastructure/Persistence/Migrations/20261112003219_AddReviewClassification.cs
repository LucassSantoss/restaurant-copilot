using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "classified_at",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sentiment",
                table: "reviews",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "themes",
                table: "reviews",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.CreateIndex(
                name: "ix_reviews_classified_at",
                table: "reviews",
                column: "classified_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_reviews_classified_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "classified_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "sentiment",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "themes",
                table: "reviews");
        }
    }
}
