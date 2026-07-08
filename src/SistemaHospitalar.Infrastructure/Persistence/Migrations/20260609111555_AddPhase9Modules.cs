using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase9Modules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chemotherapy_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProtocolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DrugRegimen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CycleNumber = table.Column<int>(type: "integer", nullable: false),
                    TotalCycles = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdministeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chemotherapy_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chemotherapy_sessions_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_chemotherapy_sessions_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chemotherapy_sessions_professionals_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "infection_surveillance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InfectionType = table.Column<int>(type: "integer", nullable: false),
                    Organism = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Site = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReportedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_infection_surveillance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_infection_surveillance_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_infection_surveillance_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "isolation_precautions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrecautionType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_isolation_precautions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_isolation_precautions_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_isolation_precautions_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "physiotherapy_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TherapistName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SessionType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Goals = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_physiotherapy_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_physiotherapy_sessions_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_physiotherapy_sessions_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "telemedicine_appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MeetingUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChiefComplaint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemedicine_appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_telemedicine_appointments_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_telemedicine_appointments_professionals_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chemotherapy_sessions_HospitalizationId",
                table: "chemotherapy_sessions",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_chemotherapy_sessions_PatientId",
                table: "chemotherapy_sessions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_chemotherapy_sessions_ProfessionalId",
                table: "chemotherapy_sessions",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_infection_surveillance_HospitalizationId",
                table: "infection_surveillance",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_infection_surveillance_PatientId",
                table: "infection_surveillance",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_isolation_precautions_HospitalizationId",
                table: "isolation_precautions",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_isolation_precautions_PatientId",
                table: "isolation_precautions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_sessions_HospitalizationId",
                table: "physiotherapy_sessions",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_sessions_PatientId",
                table: "physiotherapy_sessions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_telemedicine_appointments_PatientId",
                table: "telemedicine_appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_telemedicine_appointments_ProfessionalId",
                table: "telemedicine_appointments",
                column: "ProfessionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chemotherapy_sessions");

            migrationBuilder.DropTable(
                name: "infection_surveillance");

            migrationBuilder.DropTable(
                name: "isolation_precautions");

            migrationBuilder.DropTable(
                name: "physiotherapy_sessions");

            migrationBuilder.DropTable(
                name: "telemedicine_appointments");
        }
    }
}
