using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImprovePayrollModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConsolidatedFinancialAccountId",
                table: "payroll_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalFgtsEmployer",
                table: "payroll_runs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FgtsEmployerAmount",
                table: "payroll_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmount",
                table: "payroll_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseSalary",
                table: "employees",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PayrollNotes",
                table: "employees",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payroll_item_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineType = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_item_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_item_lines_payroll_items_PayrollItemId",
                        column: x => x.PayrollItemId,
                        principalTable: "payroll_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_runs_ConsolidatedFinancialAccountId",
                table: "payroll_runs",
                column: "ConsolidatedFinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_item_lines_PayrollItemId",
                table: "payroll_item_lines",
                column: "PayrollItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_runs_financial_accounts_ConsolidatedFinancialAccoun~",
                table: "payroll_runs",
                column: "ConsolidatedFinancialAccountId",
                principalTable: "financial_accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_runs_financial_accounts_ConsolidatedFinancialAccoun~",
                table: "payroll_runs");

            migrationBuilder.DropTable(
                name: "payroll_item_lines");

            migrationBuilder.DropIndex(
                name: "IX_payroll_runs_ConsolidatedFinancialAccountId",
                table: "payroll_runs");

            migrationBuilder.DropColumn(
                name: "ConsolidatedFinancialAccountId",
                table: "payroll_runs");

            migrationBuilder.DropColumn(
                name: "TotalFgtsEmployer",
                table: "payroll_runs");

            migrationBuilder.DropColumn(
                name: "FgtsEmployerAmount",
                table: "payroll_items");

            migrationBuilder.DropColumn(
                name: "GrossAmount",
                table: "payroll_items");

            migrationBuilder.DropColumn(
                name: "BaseSalary",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "PayrollNotes",
                table: "employees");
        }
    }
}
