using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [Migration("20260617220000_AddHospitalReferenceCatalog")]
    public partial class AddHospitalReferenceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hospital_reference_catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogType = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentGroup = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    ContentRevision = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_reference_catalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_reference_catalog_CatalogType_Code",
                table: "hospital_reference_catalog",
                columns: new[] { "CatalogType", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hospital_reference_catalog_CatalogType_ParentGroup_DisplayOrder",
                table: "hospital_reference_catalog",
                columns: new[] { "CatalogType", "ParentGroup", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hospital_reference_catalog");
        }
    }
}
