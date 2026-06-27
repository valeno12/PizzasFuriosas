using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzasFuriosas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 58, 58, 641, DateTimeKind.Utc).AddTicks(8916));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 58, 58, 641, DateTimeKind.Utc).AddTicks(8917));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 58, 58, 641, DateTimeKind.Utc).AddTicks(8918));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Email", "IsDeleted", "Name", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2026, 6, 26, 17, 58, 58, 641, DateTimeKind.Utc).AddTicks(9008), null, "admin@pizzasfuriosas.com", false, "Admin", "$2b$12$AHVX2ThuQfI1iQpiKDvRTupHytfL5KpC2T2hwR/Mbnu5RGLSBRHAm", "Admin", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 53, 51, 237, DateTimeKind.Utc).AddTicks(3361));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 53, 51, 237, DateTimeKind.Utc).AddTicks(3362));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 26, 17, 53, 51, 237, DateTimeKind.Utc).AddTicks(3363));
        }
    }
}
