using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzasFuriosas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsers2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 28, 49, 716, DateTimeKind.Utc).AddTicks(5195));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 28, 49, 716, DateTimeKind.Utc).AddTicks(5196));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 28, 49, 716, DateTimeKind.Utc).AddTicks(5202));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 18, 33, 572, DateTimeKind.Utc).AddTicks(4822));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 18, 33, 572, DateTimeKind.Utc).AddTicks(4823));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 18, 33, 572, DateTimeKind.Utc).AddTicks(4824));
        }
    }
}
