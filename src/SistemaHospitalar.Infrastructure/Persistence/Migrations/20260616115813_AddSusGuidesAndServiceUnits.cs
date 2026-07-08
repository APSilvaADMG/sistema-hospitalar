using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSusGuidesAndServiceUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceUnitId",
                table: "tiss_guides",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "service_units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Cnes = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sus_guides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuideNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GuideType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cid10Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SigtapProcedureCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ProcedureDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Competence = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    AuthorizationNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    AuthorizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sus_guides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sus_guides_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_sus_guides_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_sus_guides_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sus_guides_professionals_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_sus_guides_service_units_ServiceUnitId",
                        column: x => x.ServiceUnitId,
                        principalTable: "service_units",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tiss_guides_ServiceUnitId",
                table: "tiss_guides",
                column: "ServiceUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_service_units_Code",
                table: "service_units",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sus_guides_AppointmentId",
                table: "sus_guides",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_sus_guides_GuideNumber",
                table: "sus_guides",
                column: "GuideNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sus_guides_HospitalizationId",
                table: "sus_guides",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_sus_guides_PatientId",
                table: "sus_guides",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_sus_guides_ProfessionalId",
                table: "sus_guides",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_sus_guides_ServiceUnitId",
                table: "sus_guides",
                column: "ServiceUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_guides_service_units_ServiceUnitId",
                table: "tiss_guides",
                column: "ServiceUnitId",
                principalTable: "service_units",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tiss_guides_service_units_ServiceUnitId",
                table: "tiss_guides");

            migrationBuilder.DropTable(
                name: "sus_guides");

            migrationBuilder.DropTable(
                name: "service_units");

            migrationBuilder.DropIndex(
                name: "IX_tiss_guides_ServiceUnitId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ServiceUnitId",
                table: "tiss_guides");
        }
    }
}
