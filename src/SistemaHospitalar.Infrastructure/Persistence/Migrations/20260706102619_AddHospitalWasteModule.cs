using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalWasteModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSlaViolated",
                table: "TransportRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaDeadlineAt",
                table: "TransportRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresEligibilityCheck",
                table: "health_insurances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "HospitalEventLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalEventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WasteCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WasteType = table.Column<int>(type: "integer", nullable: false),
                    SectorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    QuantityKg = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    ContainerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CollectedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ManifestNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WasteCollections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalEventLogs_CreatedAt",
                table: "HospitalEventLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalEventLogs_EventType",
                table: "HospitalEventLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalEventLogs_RelatedEntityType_RelatedEntityId",
                table: "HospitalEventLogs",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_WasteCollections_Code",
                table: "WasteCollections",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WasteCollections_CollectedAt",
                table: "WasteCollections",
                column: "CollectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WasteCollections_WasteType",
                table: "WasteCollections",
                column: "WasteType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HospitalEventLogs");

            migrationBuilder.DropTable(
                name: "WasteCollections");

            migrationBuilder.DropColumn(
                name: "IsSlaViolated",
                table: "TransportRequests");

            migrationBuilder.DropColumn(
                name: "SlaDeadlineAt",
                table: "TransportRequests");

            migrationBuilder.DropColumn(
                name: "RequiresEligibilityCheck",
                table: "health_insurances");
        }
    }
}
