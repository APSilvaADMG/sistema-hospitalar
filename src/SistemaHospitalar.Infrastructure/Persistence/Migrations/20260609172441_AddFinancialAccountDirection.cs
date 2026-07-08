using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialAccountDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_financial_accounts_patients_PatientId",
                table: "financial_accounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "financial_accounts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyName",
                table: "financial_accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "financial_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "financial_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_SupplierId",
                table: "financial_accounts",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_financial_accounts_patients_PatientId",
                table: "financial_accounts",
                column: "PatientId",
                principalTable: "patients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_financial_accounts_suppliers_SupplierId",
                table: "financial_accounts",
                column: "SupplierId",
                principalTable: "suppliers",
                principalColumn: "Id");

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
            migrationBuilder.DropForeignKey(
                name: "FK_financial_accounts_patients_PatientId",
                table: "financial_accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_financial_accounts_suppliers_SupplierId",
                table: "financial_accounts");

            migrationBuilder.DropIndex(
                name: "IX_financial_accounts_SupplierId",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "CounterpartyName",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "financial_accounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "financial_accounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_financial_accounts_patients_PatientId",
                table: "financial_accounts",
                column: "PatientId",
                principalTable: "patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
