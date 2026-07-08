using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardPaymentInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "financial_payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "financial_accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentNumber",
                table: "financial_accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentFinancialAccountId",
                table: "financial_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "financial_payment_installments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "integer", nullable: false),
                    InstallmentCount = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_payment_installments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_financial_payment_installments_financial_accounts_Financial~",
                        column: x => x.FinancialAccountId,
                        principalTable: "financial_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_financial_payment_installments_financial_payments_Financial~",
                        column: x => x.FinancialPaymentId,
                        principalTable: "financial_payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_ParentFinancialAccountId",
                table: "financial_accounts",
                column: "ParentFinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_payment_installments_FinancialAccountId",
                table: "financial_payment_installments",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_payment_installments_FinancialPaymentId_Installme~",
                table: "financial_payment_installments",
                columns: new[] { "FinancialPaymentId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_financial_accounts_financial_accounts_ParentFinancialAccoun~",
                table: "financial_accounts",
                column: "ParentFinancialAccountId",
                principalTable: "financial_accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_financial_accounts_financial_accounts_ParentFinancialAccoun~",
                table: "financial_accounts");

            migrationBuilder.DropTable(
                name: "financial_payment_installments");

            migrationBuilder.DropIndex(
                name: "IX_financial_accounts_ParentFinancialAccountId",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "financial_payments");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "InstallmentNumber",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "ParentFinancialAccountId",
                table: "financial_accounts");
        }
    }
}
