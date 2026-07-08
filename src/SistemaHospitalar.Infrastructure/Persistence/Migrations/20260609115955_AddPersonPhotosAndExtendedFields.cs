using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonPhotosAndExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressCity",
                table: "professionals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressComplement",
                table: "professionals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressNeighborhood",
                table: "professionals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressNumber",
                table: "professionals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressState",
                table: "professionals",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressStreet",
                table: "professionals",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressZipCode",
                table: "professionals",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "professionals",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CouncilUf",
                table: "professionals",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "professionals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MobilePhone",
                table: "professionals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "professionals",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoData",
                table: "professionals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rg",
                table: "professionals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialName",
                table: "professionals",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BirthPlace",
                table: "patients",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodType",
                table: "patients",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatus",
                table: "patients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "patients",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "patients",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoData",
                table: "patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rg",
                table: "patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressCity",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressComplement",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressNeighborhood",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressNumber",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressState",
                table: "employees",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressStreet",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressZipCode",
                table: "employees",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "employees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobilePhone",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "employees",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoData",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rg",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialName",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressCity",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "AddressComplement",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "AddressNeighborhood",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "AddressNumber",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "AddressState",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "AddressStreet",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "AddressZipCode",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "CouncilUf",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "MobilePhone",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "Rg",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "SocialName",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "BirthPlace",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "BloodType",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "Rg",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "AddressCity",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "AddressComplement",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "AddressNeighborhood",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "AddressNumber",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "AddressState",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "AddressStreet",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "AddressZipCode",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "MobilePhone",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Rg",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "SocialName",
                table: "employees");
        }
    }
}
