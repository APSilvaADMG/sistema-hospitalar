using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderSectorDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Justification",
                table: "purchase_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "RequestedBy",
                table: "purchase_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Almoxarifado");

            migrationBuilder.AddColumn<int>(
                name: "Sector",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 13);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Justification",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "RequestedBy",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "purchase_orders");
        }
    }
}
