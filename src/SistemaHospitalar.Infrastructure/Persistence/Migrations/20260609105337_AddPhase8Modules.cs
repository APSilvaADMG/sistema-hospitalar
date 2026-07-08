using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase8Modules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blood_units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BloodType = table.Column<int>(type: "integer", nullable: false),
                    Component = table.Column<int>(type: "integer", nullable: false),
                    VolumeMl = table.Column<int>(type: "integer", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dialysis_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MachineNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DryWeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    NurseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dialysis_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dialysis_sessions_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_dialysis_sessions_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instrument_kits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SterilityExpiration = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instrument_kits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "laundry_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Origin = table.Column<int>(type: "integer", nullable: false),
                    OriginDetail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laundry_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transfusion_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestingProfessionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    BloodUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    BloodTypeRequired = table.Column<int>(type: "integer", nullable: false),
                    Component = table.Column<int>(type: "integer", nullable: false),
                    UnitsRequested = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TransfusedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfusion_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transfusion_requests_blood_units_BloodUnitId",
                        column: x => x.BloodUnitId,
                        principalTable: "blood_units",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_transfusion_requests_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_transfusion_requests_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transfusion_requests_professionals_RequestingProfessionalId",
                        column: x => x.RequestingProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sterilization_cycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstrumentKitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SterilizerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sterilization_cycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sterilization_cycles_instrument_kits_InstrumentKitId",
                        column: x => x.InstrumentKitId,
                        principalTable: "instrument_kits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blood_units_UnitCode",
                table: "blood_units",
                column: "UnitCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dialysis_sessions_HospitalizationId",
                table: "dialysis_sessions",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_dialysis_sessions_PatientId",
                table: "dialysis_sessions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_instrument_kits_Code",
                table: "instrument_kits",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_laundry_batches_BatchNumber",
                table: "laundry_batches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sterilization_cycles_InstrumentKitId",
                table: "sterilization_cycles",
                column: "InstrumentKitId");

            migrationBuilder.CreateIndex(
                name: "IX_transfusion_requests_BloodUnitId",
                table: "transfusion_requests",
                column: "BloodUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_transfusion_requests_HospitalizationId",
                table: "transfusion_requests",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_transfusion_requests_PatientId",
                table: "transfusion_requests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_transfusion_requests_RequestingProfessionalId",
                table: "transfusion_requests",
                column: "RequestingProfessionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dialysis_sessions");

            migrationBuilder.DropTable(
                name: "laundry_batches");

            migrationBuilder.DropTable(
                name: "sterilization_cycles");

            migrationBuilder.DropTable(
                name: "transfusion_requests");

            migrationBuilder.DropTable(
                name: "instrument_kits");

            migrationBuilder.DropTable(
                name: "blood_units");
        }
    }
}
