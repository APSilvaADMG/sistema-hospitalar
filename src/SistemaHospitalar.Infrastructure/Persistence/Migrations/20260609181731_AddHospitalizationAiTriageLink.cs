using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationAiTriageLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AiTriageLogId",
                table: "hospitalizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_hospitalizations_AiTriageLogId",
                table: "hospitalizations",
                column: "AiTriageLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_hospitalizations_ai_triage_logs_AiTriageLogId",
                table: "hospitalizations",
                column: "AiTriageLogId",
                principalTable: "ai_triage_logs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_hospitalizations_ai_triage_logs_AiTriageLogId",
                table: "hospitalizations");

            migrationBuilder.DropIndex(
                name: "IX_hospitalizations_AiTriageLogId",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "AiTriageLogId",
                table: "hospitalizations");
        }
    }
}
