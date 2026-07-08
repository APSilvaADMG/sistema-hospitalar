using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBedTransferAndPatientCns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cns",
                table: "patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bed_transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromBedId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToBedId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransferredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bed_transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bed_transfers_beds_FromBedId",
                        column: x => x.FromBedId,
                        principalTable: "beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bed_transfers_beds_ToBedId",
                        column: x => x.ToBedId,
                        principalTable: "beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bed_transfers_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bed_transfers_professionals_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_bed_transfers_FromBedId",
                table: "bed_transfers",
                column: "FromBedId");

            migrationBuilder.CreateIndex(
                name: "IX_bed_transfers_HospitalizationId",
                table: "bed_transfers",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_bed_transfers_ProfessionalId",
                table: "bed_transfers",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_bed_transfers_ToBedId",
                table: "bed_transfers",
                column: "ToBedId");

            migrationBuilder.CreateIndex(
                name: "IX_bed_transfers_TransferredAt",
                table: "bed_transfers",
                column: "TransferredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bed_transfers");

            migrationBuilder.DropColumn(
                name: "Cns",
                table: "patients");
        }
    }
}
