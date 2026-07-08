using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSigtapCompetenceVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Competence",
                table: "sigtap_procedures",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE sigtap_procedures
                SET "Competence" = COALESCE(
                    TO_CHAR("ValidFrom", 'YYYY-MM'),
                    TO_CHAR(NOW() AT TIME ZONE 'UTC', 'YYYY-MM'));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Competence",
                table: "sigtap_procedures",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_sigtap_procedures_Code",
                table: "sigtap_procedures");

            migrationBuilder.CreateIndex(
                name: "IX_sigtap_procedures_Code_Competence",
                table: "sigtap_procedures",
                columns: new[] { "Code", "Competence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sigtap_procedures_Code_Competence",
                table: "sigtap_procedures");

            migrationBuilder.DropColumn(
                name: "Competence",
                table: "sigtap_procedures");

            migrationBuilder.CreateIndex(
                name: "IX_sigtap_procedures_Code",
                table: "sigtap_procedures",
                column: "Code",
                unique: true);
        }
    }
}
