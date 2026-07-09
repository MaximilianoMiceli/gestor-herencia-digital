using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Herencia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstadoABeneficiario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Beneficiarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.UpdateData(
                table: "Beneficiarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "Estado",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Beneficiarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "Estado",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Beneficiarios",
                keyColumn: "Id",
                keyValue: 3,
                column: "Estado",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Beneficiarios",
                keyColumn: "Id",
                keyValue: 4,
                column: "Estado",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Beneficiarios");
        }
    }
}
