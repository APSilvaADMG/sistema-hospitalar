using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandMedicationBulaForConsultaRemedios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PackageInsert",
                table: "medication_catalogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalBulaSlug",
                table: "medication_catalogs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_medication_catalogs_ExternalBulaSlug",
                table: "medication_catalogs",
                column: "ExternalBulaSlug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_medication_catalogs_ExternalBulaSlug",
                table: "medication_catalogs");

            migrationBuilder.DropColumn(
                name: "ExternalBulaSlug",
                table: "medication_catalogs");

            migrationBuilder.AlterColumn<string>(
                name: "PackageInsert",
                table: "medication_catalogs",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
