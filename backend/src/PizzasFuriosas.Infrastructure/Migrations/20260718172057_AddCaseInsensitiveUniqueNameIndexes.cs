using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzasFuriosas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseInsensitiveUniqueNameIndexes : Migration
    {
        // Índices únicos "case-insensitive" para garantizar a nivel base de datos que no
        // haya nombres duplicados (ej: "Pizzas" y "pizzas"). Esto NO se puede expresar en
        // el modelo de EF (es un índice sobre la expresión lower(Name)), por eso va como SQL.
        //
        // Dos detalles clave:
        //   1) lower("Name")  -> hace el índice insensible a mayúsculas/minúsculas y encima
        //      respalda las consultas que ya hacen WHERE lower(name) = lower(@p).
        //   2) WHERE "IsDeleted" = false (índice PARCIAL) -> solo aplica a filas vivas.
        //      Sin esto, borrar (soft delete) una categoría "Pizzas" y volver a crear otra
        //      "Pizzas" chocaría contra el índice, porque la fila borrada sigue existiendo
        //      en la tabla. El índice parcial se alinea con el query filter de soft delete.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "IX_Categories_Name_Lower"
                ON "Categories" (lower("Name"))
                WHERE "IsDeleted" = false;
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "IX_Products_Name_Lower"
                ON "Products" (lower("Name"))
                WHERE "IsDeleted" = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "IX_Products_Name_Lower";""");
            migrationBuilder.Sql("""DROP INDEX "IX_Categories_Name_Lower";""");
        }
    }
}
