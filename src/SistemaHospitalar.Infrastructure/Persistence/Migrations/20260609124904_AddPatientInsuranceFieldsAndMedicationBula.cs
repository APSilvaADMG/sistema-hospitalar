using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientInsuranceFieldsAndMedicationBula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccommodationType",
                table: "patient_insurances",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardHolderName",
                table: "patient_insurances",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CnsNumber",
                table: "patient_insurances",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "patient_insurances",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ValidFrom",
                table: "patient_insurances",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageInsert",
                table: "medication_catalogs",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationType",
                table: "patient_insurances");

            migrationBuilder.DropColumn(
                name: "CardHolderName",
                table: "patient_insurances");

            migrationBuilder.DropColumn(
                name: "CnsNumber",
                table: "patient_insurances");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "patient_insurances");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "patient_insurances");

            migrationBuilder.DropColumn(
                name: "PackageInsert",
                table: "medication_catalogs");
        }
    }
}
