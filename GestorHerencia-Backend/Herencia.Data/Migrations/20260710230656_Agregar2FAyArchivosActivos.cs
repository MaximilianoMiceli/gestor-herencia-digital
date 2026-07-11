using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Herencia.Data.Migrations
{
    /// <inheritdoc />
    public partial class Agregar2FAyArchivosActivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoDobleFactor",
                table: "Usuarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoDobleFactorExpiracion",
                table: "Usuarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DobleFactorHabilitado",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NombreArchivoOriginal",
                table: "ActivosDigitales",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaArchivo",
                table: "ActivosDigitales",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ActivosDigitales",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "NombreArchivoOriginal", "RutaArchivo" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CodigoDobleFactor", "CodigoDobleFactorExpiracion", "DobleFactorHabilitado" },
                values: new object[] { null, null, false });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CodigoDobleFactor", "CodigoDobleFactorExpiracion", "DobleFactorHabilitado" },
                values: new object[] { null, null, false });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CodigoDobleFactor", "CodigoDobleFactorExpiracion", "DobleFactorHabilitado" },
                values: new object[] { null, null, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoDobleFactor",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoDobleFactorExpiracion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DobleFactorHabilitado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NombreArchivoOriginal",
                table: "ActivosDigitales");

            migrationBuilder.DropColumn(
                name: "RutaArchivo",
                table: "ActivosDigitales");
        }
    }
}
