using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialAccountDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "financial_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "HospitalizationId",
                table: "financial_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "financial_accounts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_HospitalizationId",
                table: "financial_accounts",
                column: "HospitalizationId",
                unique: true,
                filter: "\"HospitalizationId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_financial_accounts_hospitalizations_HospitalizationId",
                table: "financial_accounts",
                column: "HospitalizationId",
                principalTable: "hospitalizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_financial_accounts_hospitalizations_HospitalizationId",
                table: "financial_accounts");

            migrationBuilder.DropIndex(
                name: "IX_financial_accounts_HospitalizationId",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "HospitalizationId",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "financial_accounts");
        }
    }
}
