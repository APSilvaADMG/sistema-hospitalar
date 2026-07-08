using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnrichStockReceiptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "estoque_entradas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FreightAmount",
                table: "estoque_entradas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "InvoiceIssueDate",
                table: "estoque_entradas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceSeries",
                table: "estoque_entradas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfeAccessKey",
                table: "estoque_entradas",
                type: "character varying(44)",
                maxLength: 44,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentCondition",
                table: "estoque_entradas",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCnpj",
                table: "estoque_entradas",
                type: "character varying(18)",
                maxLength: 18,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cfop",
                table: "estoque_entrada_itens",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ncm",
                table: "estoque_entrada_itens",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_estoque_entradas_NfeAccessKey",
                table: "estoque_entradas",
                column: "NfeAccessKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_estoque_entradas_NfeAccessKey",
                table: "estoque_entradas");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "estoque_entradas");

            migrationBuilder.DropColumn(
                name: "FreightAmount",
                table: "estoque_entradas");

            migrationBuilder.DropColumn(
                name: "InvoiceIssueDate",
                table: "estoque_entradas");

            migrationBuilder.DropColumn(
                name: "InvoiceSeries",
                table: "estoque_entradas");

            migrationBuilder.DropColumn(
                name: "NfeAccessKey",
                table: "estoque_entradas");

            migrationBuilder.DropColumn(
                name: "PaymentCondition",
                table: "estoque_entradas");

            migrationBuilder.DropColumn(
                name: "SupplierCnpj",
                table: "estoque_entradas");

            migrationBuilder.DropColumn(
                name: "Cfop",
                table: "estoque_entrada_itens");

            migrationBuilder.DropColumn(
                name: "Ncm",
                table: "estoque_entrada_itens");
        }
    }
}
