using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTissProfessionalBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccidentIndicator",
                table: "tiss_guides",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdmissionDate",
                table: "tiss_guides",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cid10Code",
                table: "tiss_guides",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cid10Secondary",
                table: "tiss_guides",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClinicalJustification",
                table: "tiss_guides",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DischargeDate",
                table: "tiss_guides",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutingProfessionalCrm",
                table: "tiss_guides",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExecutingProfessionalId",
                table: "tiss_guides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutingProfessionalName",
                table: "tiss_guides",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentGuideId",
                table: "tiss_guides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ParticipationPercent",
                table: "tiss_guides",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfessionalRole",
                table: "tiss_guides",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedBedType",
                table: "tiss_guides",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestingProfessionalCrm",
                table: "tiss_guides",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestingProfessionalId",
                table: "tiss_guides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestingProfessionalName",
                table: "tiss_guides",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceCharacter",
                table: "tiss_guides",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SurgeryId",
                table: "tiss_guides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cid10Code",
                table: "tiss_guide_items",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceTableSource",
                table: "tiss_guide_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedTussCode",
                table: "tiss_guide_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorizationDeadlineDays",
                table: "health_insurances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessRules",
                table: "health_insurances",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresOnlineAuthorization",
                table: "health_insurances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "brasindice_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Laboratory = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Presentation = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ReferencePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brasindice_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cbhpm_procedures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Port = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Uco = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ReferencePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cbhpm_procedures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "simpro_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReferencePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simpro_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tiss_guides_ExecutingProfessionalId",
                table: "tiss_guides",
                column: "ExecutingProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_guides_ParentGuideId",
                table: "tiss_guides",
                column: "ParentGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_guides_RequestingProfessionalId",
                table: "tiss_guides",
                column: "RequestingProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_guides_SurgeryId",
                table: "tiss_guides",
                column: "SurgeryId");

            migrationBuilder.CreateIndex(
                name: "IX_brasindice_items_Code",
                table: "brasindice_items",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cbhpm_procedures_Code",
                table: "cbhpm_procedures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_simpro_items_Code",
                table: "simpro_items",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_guides_professionals_ExecutingProfessionalId",
                table: "tiss_guides",
                column: "ExecutingProfessionalId",
                principalTable: "professionals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_guides_professionals_RequestingProfessionalId",
                table: "tiss_guides",
                column: "RequestingProfessionalId",
                principalTable: "professionals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_guides_surgeries_SurgeryId",
                table: "tiss_guides",
                column: "SurgeryId",
                principalTable: "surgeries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_guides_tiss_guides_ParentGuideId",
                table: "tiss_guides",
                column: "ParentGuideId",
                principalTable: "tiss_guides",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tiss_guides_professionals_ExecutingProfessionalId",
                table: "tiss_guides");

            migrationBuilder.DropForeignKey(
                name: "FK_tiss_guides_professionals_RequestingProfessionalId",
                table: "tiss_guides");

            migrationBuilder.DropForeignKey(
                name: "FK_tiss_guides_surgeries_SurgeryId",
                table: "tiss_guides");

            migrationBuilder.DropForeignKey(
                name: "FK_tiss_guides_tiss_guides_ParentGuideId",
                table: "tiss_guides");

            migrationBuilder.DropTable(
                name: "brasindice_items");

            migrationBuilder.DropTable(
                name: "cbhpm_procedures");

            migrationBuilder.DropTable(
                name: "simpro_items");

            migrationBuilder.DropIndex(
                name: "IX_tiss_guides_ExecutingProfessionalId",
                table: "tiss_guides");

            migrationBuilder.DropIndex(
                name: "IX_tiss_guides_ParentGuideId",
                table: "tiss_guides");

            migrationBuilder.DropIndex(
                name: "IX_tiss_guides_RequestingProfessionalId",
                table: "tiss_guides");

            migrationBuilder.DropIndex(
                name: "IX_tiss_guides_SurgeryId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "AccidentIndicator",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "AdmissionDate",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "Cid10Code",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "Cid10Secondary",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ClinicalJustification",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "DischargeDate",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ExecutingProfessionalCrm",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ExecutingProfessionalId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ExecutingProfessionalName",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ParentGuideId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ParticipationPercent",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ProfessionalRole",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "RequestedBedType",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "RequestingProfessionalCrm",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "RequestingProfessionalId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "RequestingProfessionalName",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ServiceCharacter",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "SurgeryId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "Cid10Code",
                table: "tiss_guide_items");

            migrationBuilder.DropColumn(
                name: "PriceTableSource",
                table: "tiss_guide_items");

            migrationBuilder.DropColumn(
                name: "RelatedTussCode",
                table: "tiss_guide_items");

            migrationBuilder.DropColumn(
                name: "AuthorizationDeadlineDays",
                table: "health_insurances");

            migrationBuilder.DropColumn(
                name: "BusinessRules",
                table: "health_insurances");

            migrationBuilder.DropColumn(
                name: "RequiresOnlineAuthorization",
                table: "health_insurances");
        }
    }
}
