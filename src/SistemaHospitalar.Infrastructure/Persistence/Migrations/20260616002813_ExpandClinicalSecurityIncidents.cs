using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandClinicalSecurityIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "security_incidents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                table: "security_incidents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_incidents_PatientId",
                table: "security_incidents",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_security_incidents_patients_PatientId",
                table: "security_incidents",
                column: "PatientId",
                principalTable: "patients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_security_incidents_patients_PatientId",
                table: "security_incidents");

            migrationBuilder.DropIndex(
                name: "IX_security_incidents_PatientId",
                table: "security_incidents");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "security_incidents");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "security_incidents");
        }
    }
}
