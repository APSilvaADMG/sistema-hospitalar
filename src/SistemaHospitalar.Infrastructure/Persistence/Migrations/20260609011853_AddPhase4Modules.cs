using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase4Modules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_triage_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Symptoms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Urgency = table.Column<int>(type: "integer", nullable: false),
                    RecommendedSpecialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SuggestedCid10 = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SuggestedCid10Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_triage_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_triage_logs_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "cid10_catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cid10_catalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "integration_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Destination = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ResponsePayload = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_integration_messages_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_PatientId",
                table: "users",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_triage_logs_PatientId",
                table: "ai_triage_logs",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_cid10_catalog_Code",
                table: "cid10_catalog",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_messages_PatientId",
                table: "integration_messages",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_patients_PatientId",
                table: "users",
                column: "PatientId",
                principalTable: "patients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_patients_PatientId",
                table: "users");

            migrationBuilder.DropTable(
                name: "ai_triage_logs");

            migrationBuilder.DropTable(
                name: "cid10_catalog");

            migrationBuilder.DropTable(
                name: "integration_messages");

            migrationBuilder.DropIndex(
                name: "IX_users_PatientId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "users");
        }
    }
}
