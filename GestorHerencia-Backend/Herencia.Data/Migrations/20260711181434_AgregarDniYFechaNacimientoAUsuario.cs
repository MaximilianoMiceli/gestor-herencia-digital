using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Herencia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDniYFechaNacimientoAUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dni",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 15,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Usuarios",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // --- Backfill para filas YA EXISTENTES que no forman parte del seed ---
            // Esta migracion corre sobre bases de desarrollo que, ademas de los 3
            // usuarios sembrados en AppDbContext, pueden tener cuentas reales
            // creadas a mano durante pruebas manuales (via POST /auth/registro)
            // ANTES de que este campo existiera. Todas esas filas entran a este
            // AddColumn con el mismo defaultValue "" (ver arriba): si se creara el
            // indice UNICO de "Dni" con mas de una fila compartiendo "", la
            // migracion fallaria por violacion de restriccion. Se les asigna un
            // placeholder DISTINTO por fila (concatenando el propio Id, que ya es
            // unico por definicion) para dejar la base en un estado consistente;
            // cada dueño de una cuenta asi puede corregir su DNI real despues
            // desde "Editar perfil" (PUT /api/usuarios/{id}, que ya valida el
            // formato real de 7/8 digitos en el proximo guardado).
            migrationBuilder.Sql(
                "UPDATE \"Usuarios\" SET \"Dni\" = 'PENDIENTE' || \"Id\" WHERE \"Dni\" = '';");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Dni", "FechaNacimiento" },
                values: new object[] { "30111222", new DateTime(1988, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Dni", "FechaNacimiento" },
                values: new object[] { "28555666", new DateTime(1982, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Dni", "FechaNacimiento" },
                values: new object[] { "33999888", new DateTime(1995, 11, 5, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Dni",
                table: "Usuarios",
                column: "Dni",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Dni",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Dni",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Usuarios");
        }
    }
}
