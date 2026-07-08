using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryConfigTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_lookup_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_lookup_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medication_insurance_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescribedProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthInsuranceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_insurance_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_medication_insurance_mappings_health_insurances_HealthInsur~",
                        column: x => x.HealthInsuranceId,
                        principalTable: "health_insurances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_medication_insurance_mappings_products_PrescribedProductId",
                        column: x => x.PrescribedProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_medication_insurance_mappings_products_ReferenceProductId",
                        column: x => x.ReferenceProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lookup_items_Type_Name",
                table: "inventory_lookup_items",
                columns: new[] { "Type", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medication_insurance_mappings_HealthInsuranceId",
                table: "medication_insurance_mappings",
                column: "HealthInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_medication_insurance_mappings_PrescribedProductId_Reference~",
                table: "medication_insurance_mappings",
                columns: new[] { "PrescribedProductId", "ReferenceProductId", "HealthInsuranceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medication_insurance_mappings_ReferenceProductId",
                table: "medication_insurance_mappings",
                column: "ReferenceProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_lookup_items");

            migrationBuilder.DropTable(
                name: "medication_insurance_mappings");
        }
    }
}
