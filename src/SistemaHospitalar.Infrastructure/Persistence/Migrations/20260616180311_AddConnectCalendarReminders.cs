using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectCalendarReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderOccurrenceStart",
                table: "connect_calendar_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt",
                table: "connect_calendar_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tb_pendencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Modulo = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Prioridade = table.Column<int>(type: "integer", nullable: false),
                    Responsavel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Setor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DataAbertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataLimite = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LinkDestino = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UsuarioResponsavelId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_pendencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_pendencias_users_UsuarioResponsavelId",
                        column: x => x.UsuarioResponsavelId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_pendencias_UsuarioResponsavelId_SourceEntityType_SourceE~",
                table: "tb_pendencias",
                columns: new[] { "UsuarioResponsavelId", "SourceEntityType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_pendencias_UsuarioResponsavelId_Status_DataLimite",
                table: "tb_pendencias",
                columns: new[] { "UsuarioResponsavelId", "Status", "DataLimite" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_pendencias");

            migrationBuilder.DropColumn(
                name: "LastReminderOccurrenceStart",
                table: "connect_calendar_events");

            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "connect_calendar_events");
        }
    }
}
