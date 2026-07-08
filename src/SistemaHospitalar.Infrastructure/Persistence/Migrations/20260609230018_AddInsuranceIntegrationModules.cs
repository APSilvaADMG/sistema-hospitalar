using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInsuranceIntegrationModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorizationPassword",
                table: "tiss_guides",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryAccommodation",
                table: "tiss_guides",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryCardNumber",
                table: "tiss_guides",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryCns",
                table: "tiss_guides",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryPlanName",
                table: "tiss_guides",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TissBatchId",
                table: "tiss_guides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnsGlosaCode",
                table: "tiss_glosas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContestationNotes",
                table: "tiss_glosas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContestationStatus",
                table: "tiss_glosas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OperatorCode",
                table: "health_insurances",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalUrl",
                table: "health_insurances",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TissVersion",
                table: "health_insurances",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "insurance_authorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthInsuranceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AuthorizationNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcedureSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TissGuideId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insurance_authorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_insurance_authorizations_health_insurances_HealthInsuranceId",
                        column: x => x.HealthInsuranceId,
                        principalTable: "health_insurances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_insurance_authorizations_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_insurance_authorizations_tiss_guides_TissGuideId",
                        column: x => x.TissGuideId,
                        principalTable: "tiss_guides",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "insurance_eligibility_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthInsuranceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PlanName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CoverageSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RawResponseJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insurance_eligibility_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_insurance_eligibility_checks_health_insurances_HealthInsura~",
                        column: x => x.HealthInsuranceId,
                        principalTable: "health_insurances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_insurance_eligibility_checks_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tiss_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HealthInsuranceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Competence = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProtocolNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    XmlContent = table.Column<string>(type: "text", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GuideCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tiss_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tiss_batches_health_insurances_HealthInsuranceId",
                        column: x => x.HealthInsuranceId,
                        principalTable: "health_insurances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tiss_guides_TissBatchId",
                table: "tiss_guides",
                column: "TissBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_insurance_authorizations_HealthInsuranceId",
                table: "insurance_authorizations",
                column: "HealthInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_insurance_authorizations_PatientId",
                table: "insurance_authorizations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_insurance_authorizations_TissGuideId",
                table: "insurance_authorizations",
                column: "TissGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_insurance_eligibility_checks_HealthInsuranceId",
                table: "insurance_eligibility_checks",
                column: "HealthInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_insurance_eligibility_checks_PatientId",
                table: "insurance_eligibility_checks",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_batches_BatchNumber",
                table: "tiss_batches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tiss_batches_HealthInsuranceId",
                table: "tiss_batches",
                column: "HealthInsuranceId");

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_guides_tiss_batches_TissBatchId",
                table: "tiss_guides",
                column: "TissBatchId",
                principalTable: "tiss_batches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tiss_guides_tiss_batches_TissBatchId",
                table: "tiss_guides");

            migrationBuilder.DropTable(
                name: "insurance_authorizations");

            migrationBuilder.DropTable(
                name: "insurance_eligibility_checks");

            migrationBuilder.DropTable(
                name: "tiss_batches");

            migrationBuilder.DropIndex(
                name: "IX_tiss_guides_TissBatchId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "AuthorizationPassword",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "BeneficiaryAccommodation",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "BeneficiaryCardNumber",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "BeneficiaryCns",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "BeneficiaryPlanName",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "TissBatchId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "AnsGlosaCode",
                table: "tiss_glosas");

            migrationBuilder.DropColumn(
                name: "ContestationNotes",
                table: "tiss_glosas");

            migrationBuilder.DropColumn(
                name: "ContestationStatus",
                table: "tiss_glosas");

            migrationBuilder.DropColumn(
                name: "OperatorCode",
                table: "health_insurances");

            migrationBuilder.DropColumn(
                name: "PortalUrl",
                table: "health_insurances");

            migrationBuilder.DropColumn(
                name: "TissVersion",
                table: "health_insurances");
        }
    }
}
