using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMovementAndBillingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Account",
                table: "stock_movements",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "stock_movements",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiryDate",
                table: "stock_movements",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndividualCode",
                table: "stock_movements",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "stock_movements",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "stock_movements",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientOrSupplier",
                table: "stock_movements",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleName",
                table: "stock_movements",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "stock_movements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "stock_movements",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_billing_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceTable = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReferenceTable = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PricePfb = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Pmc = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Edition = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_billing_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_billing_rules_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_billing_rules_ProductId",
                table: "product_billing_rules",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_billing_rules");

            migrationBuilder.DropColumn(
                name: "Account",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "IndividualCode",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "PatientOrSupplier",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "ResponsibleName",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "stock_movements");
        }
    }
}
