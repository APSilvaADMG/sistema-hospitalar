using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandProductRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowOutboundFromRegister",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AveragePurchasePrice",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageSalePrice",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "products",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContentQuantity",
                table: "products",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLocation",
                table: "products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntryLocations",
                table: "products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpiryWarningDays",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumStock",
                table: "products",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PhotoData",
                table: "products",
                type: "character varying(500000)",
                maxLength: 500000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Presentation",
                table: "products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TussCode",
                table: "products",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowOutboundFromRegister",
                table: "products");

            migrationBuilder.DropColumn(
                name: "AveragePurchasePrice",
                table: "products");

            migrationBuilder.DropColumn(
                name: "AverageSalePrice",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ContentQuantity",
                table: "products");

            migrationBuilder.DropColumn(
                name: "DefaultLocation",
                table: "products");

            migrationBuilder.DropColumn(
                name: "EntryLocations",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ExpiryWarningDays",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "products");

            migrationBuilder.DropColumn(
                name: "MaximumStock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Presentation",
                table: "products");

            migrationBuilder.DropColumn(
                name: "TussCode",
                table: "products");
        }
    }
}
