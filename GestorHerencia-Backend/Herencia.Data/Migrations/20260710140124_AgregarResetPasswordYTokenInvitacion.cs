using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Herencia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarResetPasswordYTokenInvitacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiracion",
                table: "Usuarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenInvitacion",
                table: "AsignacionesHerencia",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 1,
                column: "TokenInvitacion",
                value: "seed-token-0000000000000000000000000000001");

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 2,
                column: "TokenInvitacion",
                value: "seed-token-0000000000000000000000000000002");

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 3,
                column: "TokenInvitacion",
                value: "seed-token-0000000000000000000000000000003");

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 4,
                column: "TokenInvitacion",
                value: "seed-token-0000000000000000000000000000004");

            migrationBuilder.UpdateData(
                table: "AsignacionesHerencia",
                keyColumn: "Id",
                keyValue: 5,
                column: "TokenInvitacion",
                value: "seed-token-0000000000000000000000000000005");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordResetExpiracion", "PasswordResetToken" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordResetExpiracion", "PasswordResetToken" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordResetExpiracion", "PasswordResetToken" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesHerencia_TokenInvitacion",
                table: "AsignacionesHerencia",
                column: "TokenInvitacion",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AsignacionesHerencia_TokenInvitacion",
                table: "AsignacionesHerencia");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiracion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TokenInvitacion",
                table: "AsignacionesHerencia");
        }
    }
}
