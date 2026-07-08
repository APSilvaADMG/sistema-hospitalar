using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hospitalization_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingProfessionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredWardId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreferredWardCategory = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Diagnosis = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Cid10Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedByProfessionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AiTriageLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospitalization_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hospitalization_requests_ai_triage_logs_AiTriageLogId",
                        column: x => x.AiTriageLogId,
                        principalTable: "ai_triage_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_hospitalization_requests_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_hospitalization_requests_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hospitalization_requests_professionals_RequestingProfession~",
                        column: x => x.RequestingProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hospitalization_requests_professionals_ReviewedByProfession~",
                        column: x => x.ReviewedByProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_hospitalization_requests_wards_PreferredWardId",
                        column: x => x.PreferredWardId,
                        principalTable: "wards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_requests_AiTriageLogId",
                table: "hospitalization_requests",
                column: "AiTriageLogId");

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_requests_HospitalizationId",
                table: "hospitalization_requests",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_requests_PatientId_Status",
                table: "hospitalization_requests",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_requests_PreferredWardId",
                table: "hospitalization_requests",
                column: "PreferredWardId");

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_requests_RequestedAt",
                table: "hospitalization_requests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_requests_RequestingProfessionalId",
                table: "hospitalization_requests",
                column: "RequestingProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_requests_ReviewedByProfessionalId",
                table: "hospitalization_requests",
                column: "ReviewedByProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_requests_Status",
                table: "hospitalization_requests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hospitalization_requests");
        }
    }
}
