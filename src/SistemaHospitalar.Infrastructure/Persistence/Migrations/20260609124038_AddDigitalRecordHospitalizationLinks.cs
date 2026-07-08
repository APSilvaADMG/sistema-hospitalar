using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDigitalRecordHospitalizationLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HospitalizationId",
                table: "tiss_guides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HospitalizationId",
                table: "medical_record_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tiss_guides_HospitalizationId",
                table: "tiss_guides",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_medical_record_entries_HospitalizationId",
                table: "medical_record_entries",
                column: "HospitalizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_medical_record_entries_hospitalizations_HospitalizationId",
                table: "medical_record_entries",
                column: "HospitalizationId",
                principalTable: "hospitalizations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_guides_hospitalizations_HospitalizationId",
                table: "tiss_guides",
                column: "HospitalizationId",
                principalTable: "hospitalizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_medical_record_entries_hospitalizations_HospitalizationId",
                table: "medical_record_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_tiss_guides_hospitalizations_HospitalizationId",
                table: "tiss_guides");

            migrationBuilder.DropIndex(
                name: "IX_tiss_guides_HospitalizationId",
                table: "tiss_guides");

            migrationBuilder.DropIndex(
                name: "IX_medical_record_entries_HospitalizationId",
                table: "medical_record_entries");

            migrationBuilder.DropColumn(
                name: "HospitalizationId",
                table: "tiss_guides");

            migrationBuilder.DropColumn(
                name: "HospitalizationId",
                table: "medical_record_entries");
        }
    }
}
