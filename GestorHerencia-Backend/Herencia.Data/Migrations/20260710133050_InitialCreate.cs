using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Herencia.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivosDigitales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivosDigitales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivosDigitales_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesHerencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivoDigitalId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmailInvitado = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    PorcentajeAsignado = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CondicionLiberacion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesHerencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesHerencia_ActivosDigitales_ActivoDigitalId",
                        column: x => x.ActivoDigitalId,
                        principalTable: "ActivosDigitales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsignacionesHerencia_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Email", "FechaCreacion", "FechaModificacion", "Nombre", "PasswordHash", "PasswordSalt", "Rol", "UsuarioCreacion", "UsuarioModificacion" },
                values: new object[,]
                {
                    { 1, "maximiceli@hotmail.com.ar", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Maximiliano Miceli", new byte[] { 104, 97, 115, 104, 68, 101, 80, 114, 117, 101, 98, 97, 83, 101, 109, 105, 108, 108, 97, 49, 50, 51, 52, 53, 54 }, new byte[] { 115, 97, 108, 116, 68, 101, 80, 114, 117, 101, 98, 97, 83, 101, 109, 105, 108, 108, 97, 49, 50, 51, 52, 53, 54 }, 1, "seed", null },
                    { 2, "ana.torres@example.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ana Torres", new byte[] { 104, 97, 115, 104, 68, 101, 80, 114, 117, 101, 98, 97, 83, 101, 109, 105, 108, 108, 97, 49, 50, 51, 52, 53, 54 }, new byte[] { 115, 97, 108, 116, 68, 101, 80, 114, 117, 101, 98, 97, 83, 101, 109, 105, 108, 108, 97, 49, 50, 51, 52, 53, 54 }, 0, "seed", null },
                    { 3, "carlos.sosa@example.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Carlos Sosa", new byte[] { 104, 97, 115, 104, 68, 101, 80, 114, 117, 101, 98, 97, 83, 101, 109, 105, 108, 108, 97, 49, 50, 51, 52, 53, 54 }, new byte[] { 115, 97, 108, 116, 68, 101, 80, 114, 117, 101, 98, 97, 83, 101, 109, 105, 108, 108, 97, 49, 50, 51, 52, 53, 54 }, 0, "seed", null }
                });

            migrationBuilder.InsertData(
                table: "ActivosDigitales",
                columns: new[] { "Id", "Descripcion", "FechaCreacion", "FechaModificacion", "Nombre", "Tipo", "UsuarioCreacion", "UsuarioId", "UsuarioModificacion" },
                values: new object[,]
                {
                    { 1, "Caja de ahorro en pesos", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cuenta Banco Santander", 0, "seed", 1, null },
                    { 2, "Cuenta corriente en dolares", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cuenta Banco Galicia", 0, "seed", 1, null },
                    { 3, "Perfil publico con 5000 seguidores", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Instagram personal", 1, "seed", 1, null },
                    { 4, "Perfil familiar", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Facebook personal", 1, "seed", 1, null },
                    { 5, "Wallet Ethereum", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Billetera MetaMask", 2, "seed", 1, null },
                    { 6, "Exchange con balance en USDT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Billetera Binance", 2, "seed", 1, null },
                    { 7, "Cuenta de correo con backups de fotos", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Correo Gmail principal", 3, "seed", 1, null },
                    { 8, "Cuenta de correo del trabajo anterior", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Correo Outlook laboral", 3, "seed", 1, null },
                    { 9, "Dominio registrado en un proveedor DNS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Dominio web personal", 4, "seed", 1, null },
                    { 10, "Caja de ahorro en pesos", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cuenta Banco Nacion", 0, "seed", 2, null },
                    { 11, "Plazo fijo renovable", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cuenta Banco BBVA", 0, "seed", 2, null },
                    { 12, "Perfil profesional con recomendaciones", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "LinkedIn profesional", 1, "seed", 2, null },
                    { 13, "Hardware wallet con Bitcoin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Billetera Ledger", 2, "seed", 2, null },
                    { 14, "Cuenta de respaldo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Correo Gmail secundario", 3, "seed", 2, null },
                    { 15, "Suscripcion de streaming compartida", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cuenta Netflix", 4, "seed", 2, null },
                    { 16, "Caja de ahorro en pesos", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cuenta Banco Macro", 0, "seed", 3, null },
                    { 17, "Perfil personal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cuenta X (Twitter)", 1, "seed", 3, null }
                });

            migrationBuilder.InsertData(
                table: "AsignacionesHerencia",
                columns: new[] { "Id", "ActivoDigitalId", "CondicionLiberacion", "EmailInvitado", "Estado", "FechaCreacion", "FechaModificacion", "PorcentajeAsignado", "UsuarioCreacion", "UsuarioId", "UsuarioModificacion" },
                values: new object[,]
                {
                    { 1, 1, "Certificado de defuncion + 30 dias", "ana.torres@example.com", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 50.00m, "seed", 2, null },
                    { 2, 1, "Certificado de defuncion + 30 dias", "carlos.sosa@example.com", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 50.00m, "seed", 3, null },
                    { 3, 10, "Certificado de defuncion", "carlos.sosa@example.com", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 100.00m, "seed", 3, null },
                    { 4, 5, "Certificado de defuncion", "invitado.sinregistro@example.com", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 100.00m, "seed", null, null },
                    { 5, 13, "Mayoria de edad del beneficiario", "maximiceli@hotmail.com.ar", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 100.00m, "seed", 1, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivosDigitales_UsuarioId",
                table: "ActivosDigitales",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesHerencia_ActivoDigitalId",
                table: "AsignacionesHerencia",
                column: "ActivoDigitalId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesHerencia_UsuarioId",
                table: "AsignacionesHerencia",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesHerencia");

            migrationBuilder.DropTable(
                name: "ActivosDigitales");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
