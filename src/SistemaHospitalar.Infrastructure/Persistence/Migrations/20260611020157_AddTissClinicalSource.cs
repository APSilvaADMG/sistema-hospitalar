using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTissClinicalSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tiss_clinical_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthInsuranceId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuideType = table.Column<int>(type: "integer", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChemotherapySessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FormDataJson = table.Column<string>(type: "text", nullable: false),
                    GeneratedTissGuideId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tiss_clinical_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tiss_clinical_sources_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tiss_clinical_sources_chemotherapy_sessions_ChemotherapySes~",
                        column: x => x.ChemotherapySessionId,
                        principalTable: "chemotherapy_sessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tiss_clinical_sources_health_insurances_HealthInsuranceId",
                        column: x => x.HealthInsuranceId,
                        principalTable: "health_insurances",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tiss_clinical_sources_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tiss_clinical_sources_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tiss_clinical_sources_tiss_guides_GeneratedTissGuideId",
                        column: x => x.GeneratedTissGuideId,
                        principalTable: "tiss_guides",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_AppointmentId",
                table: "tiss_clinical_sources",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_ChemotherapySessionId",
                table: "tiss_clinical_sources",
                column: "ChemotherapySessionId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_GeneratedTissGuideId",
                table: "tiss_clinical_sources",
                column: "GeneratedTissGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_HealthInsuranceId",
                table: "tiss_clinical_sources",
                column: "HealthInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_HospitalizationId",
                table: "tiss_clinical_sources",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_PatientId_GuideType_AppointmentId_Hos~",
                table: "tiss_clinical_sources",
                columns: new[] { "PatientId", "GuideType", "AppointmentId", "HospitalizationId", "ChemotherapySessionId" },
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tiss_clinical_sources");
        }
    }
}
