using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelariaCleaningModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CleaningRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BedId = table.Column<Guid>(type: "uuid", nullable: false),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CleaningType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TriggerReason = table.Column<int>(type: "integer", nullable: false),
                    AssignedTeam = table.Column<string>(type: "text", nullable: true),
                    AssignedEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChecklistJson = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CleaningRequests_beds_BedId",
                        column: x => x.BedId,
                        principalTable: "beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CleaningRequests_employees_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CleaningRequests_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CleaningRequests_AssignedEmployeeId",
                table: "CleaningRequests",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningRequests_BedId",
                table: "CleaningRequests",
                column: "BedId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningRequests_HospitalizationId",
                table: "CleaningRequests",
                column: "HospitalizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CleaningRequests");
        }
    }
}
