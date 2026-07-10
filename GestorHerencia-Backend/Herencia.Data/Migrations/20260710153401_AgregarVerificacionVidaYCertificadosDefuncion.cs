using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Herencia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVerificacionVidaYCertificadosDefuncion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaLiberacion",
                table: "AsignacionesHerencia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CertificadosDefuncion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioTitularId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubidoPorUsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    RutaArchivo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    NombreArchivoOriginal = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    RevisadoPorUsuarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaRevision = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificadosDefuncion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificadosDefuncion_Usuarios_RevisadoPorUsuarioId",
                        column: x => x.RevisadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CertificadosDefuncion_Usuarios_SubidoPorUsuarioId",
                        column: x => x.SubidoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CertificadosDefuncion_Usuarios_UsuarioTitularId",
                        column: x => x.UsuarioTitularId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesVerificacionVida",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FrecuenciaMeses = table.Column<int>(type: "INTEGER", nullable: false),
                    Metodo = table.Column<int>(type: "INTEGER", nullable: false),
                    ContactoConfianzaId = table.Column<int>(type: "INTEGER", nullable: true),
                    UltimoCheckIn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    RecordatoriosEnviados = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaUltimoRecordatorio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaProtocoloActivado = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesVerificacionVida", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesVerificacionVida_Usuarios_ContactoConfianzaId",
                        column: x => x.ContactoConfianzaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesVerificacionVida_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaLiberacion",
                value: null);

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaLiberacion",
                value: null);

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaLiberacion",
                value: null);

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 4,
                column: "FechaLiberacion",
                value: null);

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 5,
                column: "FechaLiberacion",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosDefuncion_RevisadoPorUsuarioId",
                table: "CertificadosDefuncion",
                column: "RevisadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosDefuncion_SubidoPorUsuarioId",
                table: "CertificadosDefuncion",
                column: "SubidoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosDefuncion_UsuarioTitularId",
                table: "CertificadosDefuncion",
                column: "UsuarioTitularId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesVerificacionVida_ContactoConfianzaId",
                table: "ConfiguracionesVerificacionVida",
                column: "ContactoConfianzaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificadosDefuncion");

            migrationBuilder.DropTable(
                name: "ConfiguracionesVerificacionVida");

            migrationBuilder.DropColumn(
                name: "FechaLiberacion",
                table: "AsignacionesHerencia");
        }
    }
}
