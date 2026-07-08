using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTissExtendedModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PortalUrl",
                table: "health_insurances",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrationSecret",
                table: "health_insurances",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrationUser",
                table: "health_insurances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseMockIntegration",
                table: "health_insurances",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "WebServiceUrl",
                table: "health_insurances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HealthInsuranceId",
                table: "financial_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TissGuideId",
                table: "financial_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "operator_transaction_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthInsuranceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    RequestPayload = table.Column<string>(type: "text", nullable: true),
                    ResponsePayload = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operator_transaction_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_operator_transaction_logs_health_insurances_HealthInsurance~",
                        column: x => x.HealthInsuranceId,
                        principalTable: "health_insurances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sigtap_procedures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    GroupName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Complexity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HospitalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ProfessionalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sigtap_procedures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tiss_demonstrativos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthInsuranceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DemonstrativoNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Competence = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalBilled = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalGlosa = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RawContent = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tiss_demonstrativos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tiss_demonstrativos_health_insurances_HealthInsuranceId",
                        column: x => x.HealthInsuranceId,
                        principalTable: "health_insurances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tiss_guide_annexes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TissGuideId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnexType = table.Column<int>(type: "integer", nullable: false),
                    Cid10Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ClinicalIndication = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CycleInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tiss_guide_annexes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tiss_guide_annexes_tiss_guides_TissGuideId",
                        column: x => x.TissGuideId,
                        principalTable: "tiss_guides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tuss_catalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TableType = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReferencePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tuss_catalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tiss_demonstrativo_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TissDemonstrativoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TissGuideId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuideNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TussCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BilledAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GlosaAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GlosaReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AnsGlosaCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsMatched = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tiss_demonstrativo_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tiss_demonstrativo_items_tiss_demonstrativos_TissDemonstrat~",
                        column: x => x.TissDemonstrativoId,
                        principalTable: "tiss_demonstrativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tiss_demonstrativo_items_tiss_guides_TissGuideId",
                        column: x => x.TissGuideId,
                        principalTable: "tiss_guides",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tiss_opme_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TissGuideAnnexId = table.Column<Guid>(type: "uuid", nullable: false),
                    TussCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AuthorizationNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tiss_opme_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tiss_opme_items_tiss_guide_annexes_TissGuideAnnexId",
                        column: x => x.TissGuideAnnexId,
                        principalTable: "tiss_guide_annexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_HealthInsuranceId",
                table: "financial_accounts",
                column: "HealthInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_TissGuideId",
                table: "financial_accounts",
                column: "TissGuideId",
                unique: true,
                filter: "\"TissGuideId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_operator_transaction_logs_HealthInsuranceId",
                table: "operator_transaction_logs",
                column: "HealthInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_sigtap_procedures_Code",
                table: "sigtap_procedures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tiss_demonstrativo_items_TissDemonstrativoId",
                table: "tiss_demonstrativo_items",
                column: "TissDemonstrativoId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_demonstrativo_items_TissGuideId",
                table: "tiss_demonstrativo_items",
                column: "TissGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_demonstrativos_HealthInsuranceId",
                table: "tiss_demonstrativos",
                column: "HealthInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_guide_annexes_TissGuideId",
                table: "tiss_guide_annexes",
                column: "TissGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_opme_items_TissGuideAnnexId",
                table: "tiss_opme_items",
                column: "TissGuideAnnexId");

            migrationBuilder.CreateIndex(
                name: "IX_tuss_catalogs_Code",
                table: "tuss_catalogs",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_financial_accounts_health_insurances_HealthInsuranceId",
                table: "financial_accounts",
                column: "HealthInsuranceId",
                principalTable: "health_insurances",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_financial_accounts_tiss_guides_TissGuideId",
                table: "financial_accounts",
                column: "TissGuideId",
                principalTable: "tiss_guides",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_financial_accounts_health_insurances_HealthInsuranceId",
                table: "financial_accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_financial_accounts_tiss_guides_TissGuideId",
                table: "financial_accounts");

            migrationBuilder.DropTable(
                name: "operator_transaction_logs");

            migrationBuilder.DropTable(
                name: "sigtap_procedures");

            migrationBuilder.DropTable(
                name: "tiss_demonstrativo_items");

            migrationBuilder.DropTable(
                name: "tiss_opme_items");

            migrationBuilder.DropTable(
                name: "tuss_catalogs");

            migrationBuilder.DropTable(
                name: "tiss_demonstrativos");

            migrationBuilder.DropTable(
                name: "tiss_guide_annexes");

            migrationBuilder.DropIndex(
                name: "IX_financial_accounts_HealthInsuranceId",
                table: "financial_accounts");

            migrationBuilder.DropIndex(
                name: "IX_financial_accounts_TissGuideId",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "IntegrationSecret",
                table: "health_insurances");

            migrationBuilder.DropColumn(
                name: "IntegrationUser",
                table: "health_insurances");

            migrationBuilder.DropColumn(
                name: "UseMockIntegration",
                table: "health_insurances");

            migrationBuilder.DropColumn(
                name: "WebServiceUrl",
                table: "health_insurances");

            migrationBuilder.DropColumn(
                name: "HealthInsuranceId",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "TissGuideId",
                table: "financial_accounts");

            migrationBuilder.AlterColumn<string>(
                name: "PortalUrl",
                table: "health_insurances",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
