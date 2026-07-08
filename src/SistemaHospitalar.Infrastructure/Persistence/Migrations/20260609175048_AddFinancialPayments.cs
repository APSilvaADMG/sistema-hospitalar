using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "financial_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PixChargeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_financial_payments_PixCharges_PixChargeId",
                        column: x => x.PixChargeId,
                        principalTable: "PixCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_financial_payments_financial_accounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalTable: "financial_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_payments_FinancialAccountId_PaidAt",
                table: "financial_payments",
                columns: new[] { "FinancialAccountId", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_payments_PixChargeId",
                table: "financial_payments",
                column: "PixChargeId");

            migrationBuilder.Sql("""
                INSERT INTO financial_payments ("Id", "FinancialAccountId", "Amount", "Method", "PaidAt", "Notes", "PixChargeId", "CreatedAt", "UpdatedAt", "IsActive")
                SELECT gen_random_uuid(),
                       fa."Id",
                       fa."PaidAmount",
                       CASE WHEN fa."Direction" = 2 THEN 5 ELSE 2 END,
                       COALESCE(fa."PaidAt", fa."UpdatedAt", fa."CreatedAt", NOW() AT TIME ZONE 'UTC'),
                       'Migrado — pagamento registrado antes do histórico',
                       NULL,
                       NOW() AT TIME ZONE 'UTC',
                       NULL,
                       TRUE
                FROM financial_accounts fa
                WHERE fa."PaidAmount" > 0
                  AND fa."IsActive" = TRUE
                  AND NOT EXISTS (
                      SELECT 1 FROM financial_payments fp WHERE fp."FinancialAccountId" = fa."Id"
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_payments");
        }
    }
}
