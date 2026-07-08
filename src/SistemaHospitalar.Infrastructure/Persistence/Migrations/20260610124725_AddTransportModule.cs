using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransportAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    AssetTag = table.Column<string>(type: "text", nullable: false),
                    AssetType = table.Column<int>(type: "integer", nullable: false),
                    Sector = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TrackingCode = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatientName = table.Column<string>(type: "text", nullable: false),
                    OriginType = table.Column<int>(type: "integer", nullable: false),
                    OriginDetail = table.Column<string>(type: "text", nullable: true),
                    DestinationType = table.Column<int>(type: "integer", nullable: false),
                    DestinationDetail = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    AssignedEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransportAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArrivedAtOriginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DepartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArrivedAtDestinationAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RequestedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportRequests_TransportAssets_TransportAssetId",
                        column: x => x.TransportAssetId,
                        principalTable: "TransportAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransportRequests_employees_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransportRequests_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransportRequests_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransportRequests_AssignedEmployeeId",
                table: "TransportRequests",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportRequests_HospitalizationId",
                table: "TransportRequests",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportRequests_PatientId",
                table: "TransportRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportRequests_TransportAssetId",
                table: "TransportRequests",
                column: "TransportAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransportRequests");

            migrationBuilder.DropTable(
                name: "TransportAssets");
        }
    }
}
