using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientLegalResponsible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_patients_CpfHash",
                table: "patients");

            migrationBuilder.AddColumn<string>(
                name: "LegalAuthorizationDocumentReference",
                table: "patients",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LegalAuthorizationDocumentType",
                table: "patients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LegalResponsibleBirthDate",
                table: "patients",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalResponsibleName",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LegalResponsibleRelationship",
                table: "patients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalResponsibleRg",
                table: "patients",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsesResponsibleCpf",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_patients_CpfHash",
                table: "patients",
                column: "CpfHash",
                unique: true,
                filter: "\"UsesResponsibleCpf\" = false AND \"CpfHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_patients_CpfHash",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "LegalAuthorizationDocumentReference",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "LegalAuthorizationDocumentType",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "LegalResponsibleBirthDate",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "LegalResponsibleName",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "LegalResponsibleRelationship",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "LegalResponsibleRg",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "UsesResponsibleCpf",
                table: "patients");

            migrationBuilder.CreateIndex(
                name: "IX_patients_CpfHash",
                table: "patients",
                column: "CpfHash",
                unique: true);
        }
    }
}
