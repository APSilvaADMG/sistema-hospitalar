using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWardModalityAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "wards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "wards",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverageModality",
                table: "wards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_wards_Code",
                table: "wards",
                column: "Code",
                unique: true,
                filter: "\"Code\" IS NOT NULL");

            migrationBuilder.Sql(
                "UPDATE wards SET \"CoverageModality\" = 4, \"Category\" = 1 WHERE \"CoverageModality\" = 0;");

            migrationBuilder.Sql(
                "UPDATE wards SET \"Category\" = 3 WHERE \"Name\" ILIKE '%UTI%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wards_Code",
                table: "wards");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "wards");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "wards");

            migrationBuilder.DropColumn(
                name: "CoverageModality",
                table: "wards");
        }
    }
}
