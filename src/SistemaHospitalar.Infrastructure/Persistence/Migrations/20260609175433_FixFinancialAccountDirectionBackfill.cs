using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixFinancialAccountDirectionBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE financial_accounts
                SET "Direction" = 1
                WHERE "Direction" = 0 AND "PatientId" IS NOT NULL;

                UPDATE financial_accounts
                SET "Direction" = 2
                WHERE "Direction" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
