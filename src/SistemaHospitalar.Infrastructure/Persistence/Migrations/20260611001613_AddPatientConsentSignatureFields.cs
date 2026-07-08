using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientConsentSignatureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "patient_consents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "patient_consents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureHash",
                table: "patient_consents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureImage",
                table: "patient_consents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignerName",
                table: "patient_consents",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "patient_consents");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "patient_consents");

            migrationBuilder.DropColumn(
                name: "SignatureHash",
                table: "patient_consents");

            migrationBuilder.DropColumn(
                name: "SignatureImage",
                table: "patient_consents");

            migrationBuilder.DropColumn(
                name: "SignerName",
                table: "patient_consents");
        }
    }
}
