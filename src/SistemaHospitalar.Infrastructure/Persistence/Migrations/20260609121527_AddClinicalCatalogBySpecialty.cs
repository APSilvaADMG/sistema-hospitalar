using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalCatalogBySpecialty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "lab_exam_catalogs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGeneral",
                table: "lab_exam_catalogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "imaging_procedure_catalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TussCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Modality = table.Column<int>(type: "integer", nullable: false),
                    BodyPart = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsGeneral = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imaging_procedure_catalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lab_exam_catalog_specialties",
                columns: table => new
                {
                    LabExamCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialtyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_exam_catalog_specialties", x => new { x.LabExamCatalogId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_lab_exam_catalog_specialties_lab_exam_catalogs_LabExamCatal~",
                        column: x => x.LabExamCatalogId,
                        principalTable: "lab_exam_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lab_exam_catalog_specialties_specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medication_catalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActiveIngredient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PharmaceuticalForm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Strength = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DefaultDosage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Route = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsGeneral = table.Column<bool>(type: "boolean", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_catalogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_medication_catalogs_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "imaging_procedure_specialties",
                columns: table => new
                {
                    ImagingProcedureCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialtyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imaging_procedure_specialties", x => new { x.ImagingProcedureCatalogId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_imaging_procedure_specialties_imaging_procedure_catalogs_Im~",
                        column: x => x.ImagingProcedureCatalogId,
                        principalTable: "imaging_procedure_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_imaging_procedure_specialties_specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medication_catalog_specialties",
                columns: table => new
                {
                    MedicationCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialtyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_catalog_specialties", x => new { x.MedicationCatalogId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_medication_catalog_specialties_medication_catalogs_Medicati~",
                        column: x => x.MedicationCatalogId,
                        principalTable: "medication_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_medication_catalog_specialties_specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_imaging_procedure_specialties_SpecialtyId",
                table: "imaging_procedure_specialties",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_lab_exam_catalog_specialties_SpecialtyId",
                table: "lab_exam_catalog_specialties",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_medication_catalog_specialties_SpecialtyId",
                table: "medication_catalog_specialties",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_medication_catalogs_ProductId",
                table: "medication_catalogs",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "imaging_procedure_specialties");

            migrationBuilder.DropTable(
                name: "lab_exam_catalog_specialties");

            migrationBuilder.DropTable(
                name: "medication_catalog_specialties");

            migrationBuilder.DropTable(
                name: "imaging_procedure_catalogs");

            migrationBuilder.DropTable(
                name: "medication_catalogs");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "lab_exam_catalogs");

            migrationBuilder.DropColumn(
                name: "IsGeneral",
                table: "lab_exam_catalogs");
        }
    }
}
