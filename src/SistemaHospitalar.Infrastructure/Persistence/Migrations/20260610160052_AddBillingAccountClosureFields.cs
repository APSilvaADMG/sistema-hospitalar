using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAccountClosureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccountClosedAt",
                table: "tiss_guides",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAudited",
                table: "tiss_guide_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingAccountClosedAt",
                table: "hospitalizations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountClosedAt",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "IsAudited",
                table: "tiss_guide_items");

            migrationBuilder.DropColumn(
                name: "BillingAccountClosedAt",
                table: "hospitalizations");
        }
    }
}
