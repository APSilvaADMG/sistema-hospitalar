using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPepSignatureAndOfflineKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "tiss_guides",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "medical_record_entries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSigned",
                table: "medical_record_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SignatureHash",
                table: "medical_record_entries",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureImage",
                table: "medical_record_entries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "medical_record_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SignedByProfessionalId",
                table: "medical_record_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tiss_guides_ClientRequestId",
                table: "tiss_guides",
                column: "ClientRequestId",
                unique: true,
                filter: "\"ClientRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_medical_record_entries_ClientRequestId",
                table: "medical_record_entries",
                column: "ClientRequestId",
                unique: true,
                filter: "\"ClientRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_medical_record_entries_SignedByProfessionalId",
                table: "medical_record_entries",
                column: "SignedByProfessionalId");

            migrationBuilder.AddForeignKey(
                name: "FK_medical_record_entries_professionals_SignedByProfessionalId",
                table: "medical_record_entries",
                column: "SignedByProfessionalId",
                principalTable: "professionals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_medical_record_entries_professionals_SignedByProfessionalId",
                table: "medical_record_entries");

            migrationBuilder.DropIndex(
                name: "IX_tiss_guides_ClientRequestId",
                table: "tiss_guides");

            migrationBuilder.DropIndex(
                name: "IX_medical_record_entries_ClientRequestId",
                table: "medical_record_entries");

            migrationBuilder.DropIndex(
                name: "IX_medical_record_entries_SignedByProfessionalId",
                table: "medical_record_entries");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "medical_record_entries");

            migrationBuilder.DropColumn(
                name: "IsSigned",
                table: "medical_record_entries");

            migrationBuilder.DropColumn(
                name: "SignatureHash",
                table: "medical_record_entries");

            migrationBuilder.DropColumn(
                name: "SignatureImage",
                table: "medical_record_entries");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "medical_record_entries");

            migrationBuilder.DropColumn(
                name: "SignedByProfessionalId",
                table: "medical_record_entries");
        }
    }
}
