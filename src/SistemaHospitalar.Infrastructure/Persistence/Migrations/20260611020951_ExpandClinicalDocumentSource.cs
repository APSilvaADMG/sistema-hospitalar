using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandClinicalDocumentSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tiss_clinical_sources_PatientId_GuideType_AppointmentId_Hos~",
                table: "tiss_clinical_sources");

            migrationBuilder.AddColumn<int>(
                name: "DocumentKind",
                table: "tiss_clinical_sources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedArtifactJson",
                table: "tiss_clinical_sources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImagingStudyId",
                table: "tiss_clinical_sources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LabOrderId",
                table: "tiss_clinical_sources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportCode",
                table: "tiss_clinical_sources",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SurgeryId",
                table: "tiss_clinical_sources",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_ImagingStudyId",
                table: "tiss_clinical_sources",
                column: "ImagingStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_LabOrderId",
                table: "tiss_clinical_sources",
                column: "LabOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_PatientId",
                table: "tiss_clinical_sources",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_SurgeryId",
                table: "tiss_clinical_sources",
                column: "SurgeryId");

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_clinical_sources_imaging_studies_ImagingStudyId",
                table: "tiss_clinical_sources",
                column: "ImagingStudyId",
                principalTable: "imaging_studies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_clinical_sources_lab_orders_LabOrderId",
                table: "tiss_clinical_sources",
                column: "LabOrderId",
                principalTable: "lab_orders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tiss_clinical_sources_surgeries_SurgeryId",
                table: "tiss_clinical_sources",
                column: "SurgeryId",
                principalTable: "surgeries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tiss_clinical_sources_imaging_studies_ImagingStudyId",
                table: "tiss_clinical_sources");

            migrationBuilder.DropForeignKey(
                name: "FK_tiss_clinical_sources_lab_orders_LabOrderId",
                table: "tiss_clinical_sources");

            migrationBuilder.DropForeignKey(
                name: "FK_tiss_clinical_sources_surgeries_SurgeryId",
                table: "tiss_clinical_sources");

            migrationBuilder.DropIndex(
                name: "IX_tiss_clinical_sources_ImagingStudyId",
                table: "tiss_clinical_sources");

            migrationBuilder.DropIndex(
                name: "IX_tiss_clinical_sources_LabOrderId",
                table: "tiss_clinical_sources");

            migrationBuilder.DropIndex(
                name: "IX_tiss_clinical_sources_PatientId",
                table: "tiss_clinical_sources");

            migrationBuilder.DropIndex(
                name: "IX_tiss_clinical_sources_SurgeryId",
                table: "tiss_clinical_sources");

            migrationBuilder.DropColumn(
                name: "DocumentKind",
                table: "tiss_clinical_sources");

            migrationBuilder.DropColumn(
                name: "GeneratedArtifactJson",
                table: "tiss_clinical_sources");

            migrationBuilder.DropColumn(
                name: "ImagingStudyId",
                table: "tiss_clinical_sources");

            migrationBuilder.DropColumn(
                name: "LabOrderId",
                table: "tiss_clinical_sources");

            migrationBuilder.DropColumn(
                name: "ReportCode",
                table: "tiss_clinical_sources");

            migrationBuilder.DropColumn(
                name: "SurgeryId",
                table: "tiss_clinical_sources");

            migrationBuilder.CreateIndex(
                name: "IX_tiss_clinical_sources_PatientId_GuideType_AppointmentId_Hos~",
                table: "tiss_clinical_sources",
                columns: new[] { "PatientId", "GuideType", "AppointmentId", "HospitalizationId", "ChemotherapySessionId" },
                unique: true,
                filter: "\"IsActive\" = true");
        }
    }
}
