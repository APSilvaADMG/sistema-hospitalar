using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationSusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AihExportedAt",
                table: "hospitalizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AihNumber",
                table: "hospitalizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CnesCode",
                table: "hospitalizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryCid10Code",
                table: "hospitalizations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimarySigtapProcedureCode",
                table: "hospitalizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryCid10Code",
                table: "hospitalizations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondarySigtapProcedureCode",
                table: "hospitalizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SusAuthorizationNumber",
                table: "hospitalizations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SusCharacter",
                table: "hospitalizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SusCompetence",
                table: "hospitalizations",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SusModality",
                table: "hospitalizations",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AihExportedAt",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "AihNumber",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "CnesCode",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "PrimaryCid10Code",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "PrimarySigtapProcedureCode",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "SecondaryCid10Code",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "SecondarySigtapProcedureCode",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "SusAuthorizationNumber",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "SusCharacter",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "SusCompetence",
                table: "hospitalizations");

            migrationBuilder.DropColumn(
                name: "SusModality",
                table: "hospitalizations");
        }
    }
}
