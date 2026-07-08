using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientDeceasedAndSurgeryOms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConsentConfirmed",
                table: "surgeries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OmsSignInCompleted",
                table: "surgeries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OmsSignOutCompleted",
                table: "surgeries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OmsTimeOutCompleted",
                table: "surgeries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeceasedAt",
                table: "patients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeceased",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentConfirmed",
                table: "surgeries");

            migrationBuilder.DropColumn(
                name: "OmsSignInCompleted",
                table: "surgeries");

            migrationBuilder.DropColumn(
                name: "OmsSignOutCompleted",
                table: "surgeries");

            migrationBuilder.DropColumn(
                name: "OmsTimeOutCompleted",
                table: "surgeries");

            migrationBuilder.DropColumn(
                name: "DeceasedAt",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "IsDeceased",
                table: "patients");
        }
    }
}
