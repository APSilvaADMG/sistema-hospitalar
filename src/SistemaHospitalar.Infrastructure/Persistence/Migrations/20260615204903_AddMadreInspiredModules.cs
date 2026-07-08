using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMadreInspiredModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReversedQuantity",
                table: "pharmacy_dispensings",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ParentCode",
                table: "cid10_catalog",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "administration_route_catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Abbreviation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administration_route_catalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bed_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BedId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bed_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bed_events_beds_BedId",
                        column: x => x.BedId,
                        principalTable: "beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bed_events_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_bed_events_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "patient_reference_catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogType = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_reference_catalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_dispensing_reversals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispensingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReversedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmacy_dispensing_reversals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pharmacy_dispensing_reversals_pharmacy_dispensings_Dispensi~",
                        column: x => x.DispensingId,
                        principalTable: "pharmacy_dispensings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cid10_catalog_ParentCode",
                table: "cid10_catalog",
                column: "ParentCode");

            migrationBuilder.CreateIndex(
                name: "IX_administration_route_catalog_Code",
                table: "administration_route_catalog",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bed_events_BedId_EndAt",
                table: "bed_events",
                columns: new[] { "BedId", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_bed_events_EventType_StartAt",
                table: "bed_events",
                columns: new[] { "EventType", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_bed_events_HospitalizationId",
                table: "bed_events",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_bed_events_PatientId",
                table: "bed_events",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_patient_reference_catalog_CatalogType_Code",
                table: "patient_reference_catalog",
                columns: new[] { "CatalogType", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_dispensing_reversals_DispensingId",
                table: "pharmacy_dispensing_reversals",
                column: "DispensingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "administration_route_catalog");

            migrationBuilder.DropTable(
                name: "bed_events");

            migrationBuilder.DropTable(
                name: "patient_reference_catalog");

            migrationBuilder.DropTable(
                name: "pharmacy_dispensing_reversals");

            migrationBuilder.DropIndex(
                name: "IX_cid10_catalog_ParentCode",
                table: "cid10_catalog");

            migrationBuilder.DropColumn(
                name: "ReversedQuantity",
                table: "pharmacy_dispensings");

            migrationBuilder.DropColumn(
                name: "ParentCode",
                table: "cid10_catalog");
        }
    }
}
