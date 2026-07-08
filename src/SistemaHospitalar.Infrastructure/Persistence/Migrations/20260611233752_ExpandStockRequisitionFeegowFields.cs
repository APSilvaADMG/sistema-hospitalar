using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandStockRequisitionFeegowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "stock_requisitions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "stock_requisitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "stock_requisitions",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "stock_requisitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItemStatus",
                table: "stock_requisition_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "stock_requisition_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "stock_requisition_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_stock_requisitions_SequenceNumber",
                table: "stock_requisitions",
                column: "SequenceNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_requisitions_SequenceNumber",
                table: "stock_requisitions");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "stock_requisitions");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "stock_requisitions");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "stock_requisitions");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "stock_requisitions");

            migrationBuilder.DropColumn(
                name: "ItemStatus",
                table: "stock_requisition_items");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "stock_requisition_items");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "stock_requisition_items");
        }
    }
}
