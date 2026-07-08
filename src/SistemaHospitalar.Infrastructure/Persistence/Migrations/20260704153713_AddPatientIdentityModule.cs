using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientIdentityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "patient_identities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdentityType = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LabelContext = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_identities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_patient_identities_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_patient_identities_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_patient_identities_Code",
                table: "patient_identities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patient_identities_HospitalizationId",
                table: "patient_identities",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_patient_identities_PatientId_IdentityType_IsActive",
                table: "patient_identities",
                columns: new[] { "PatientId", "IdentityType", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patient_identities");
        }
    }
}
