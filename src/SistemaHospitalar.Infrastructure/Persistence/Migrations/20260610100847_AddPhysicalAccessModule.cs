using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalAccessModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Building = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Floor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RequiresAuthorization = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "facial_biometric_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonType = table.Column<int>(type: "integer", nullable: false),
                    PersonName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProfessionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PhotoData = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facial_biometric_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_facial_biometric_templates_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_facial_biometric_templates_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_facial_biometric_templates_professionals_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "kiosk_tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketType = table.Column<int>(type: "integer", nullable: false),
                    TicketNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Sector = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Called = table.Column<bool>(type: "boolean", nullable: false),
                    CalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kiosk_tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "registered_vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Plate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Color = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    OwnerCategory = table.Column<int>(type: "integer", nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParkingExempt = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registered_vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registered_vehicles_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_registered_vehicles_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "access_credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonType = table.Column<int>(type: "integer", nullable: false),
                    HolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisitorLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    CredentialType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AllowedZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisitStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    VisitEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxDailyUses = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_access_credentials_access_zones_AllowedZoneId",
                        column: x => x.AllowedZoneId,
                        principalTable: "access_zones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_access_credentials_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_access_credentials_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_access_credentials_visitor_logs_VisitorLogId",
                        column: x => x.VisitorLogId,
                        principalTable: "visitor_logs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "access_turnstiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AccessZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    IntegrationVendor = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsEntry = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_turnstiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_access_turnstiles_access_zones_AccessZoneId",
                        column: x => x.AccessZoneId,
                        principalTable: "access_zones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "lpr_read_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Plate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CameraLocation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    GateOpened = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredVehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParkingSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerName = table.Column<string>(type: "text", nullable: true),
                    OwnerCategory = table.Column<int>(type: "integer", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lpr_read_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lpr_read_events_parking_sessions_ParkingSessionId",
                        column: x => x.ParkingSessionId,
                        principalTable: "parking_sessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_lpr_read_events_registered_vehicles_RegisteredVehicleId",
                        column: x => x.RegisteredVehicleId,
                        principalTable: "registered_vehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "access_control_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonType = table.Column<int>(type: "integer", nullable: false),
                    PersonName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisitorLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    TurnstileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_control_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_access_control_records_access_turnstiles_TurnstileId",
                        column: x => x.TurnstileId,
                        principalTable: "access_turnstiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_access_control_records_access_zones_AccessZoneId",
                        column: x => x.AccessZoneId,
                        principalTable: "access_zones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_control_records_AccessZoneId",
                table: "access_control_records",
                column: "AccessZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_access_control_records_OccurredAt",
                table: "access_control_records",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_access_control_records_TurnstileId",
                table: "access_control_records",
                column: "TurnstileId");

            migrationBuilder.CreateIndex(
                name: "IX_access_credentials_AllowedZoneId",
                table: "access_credentials",
                column: "AllowedZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_access_credentials_EmployeeId",
                table: "access_credentials",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_access_credentials_PatientId",
                table: "access_credentials",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_access_credentials_Token",
                table: "access_credentials",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_access_credentials_VisitorLogId",
                table: "access_credentials",
                column: "VisitorLogId");

            migrationBuilder.CreateIndex(
                name: "IX_access_turnstiles_AccessZoneId",
                table: "access_turnstiles",
                column: "AccessZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_access_turnstiles_Code",
                table: "access_turnstiles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_access_zones_Code",
                table: "access_zones",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_facial_biometric_templates_EmployeeId",
                table: "facial_biometric_templates",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_facial_biometric_templates_PatientId",
                table: "facial_biometric_templates",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_facial_biometric_templates_ProfessionalId",
                table: "facial_biometric_templates",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_facial_biometric_templates_TemplateHash",
                table: "facial_biometric_templates",
                column: "TemplateHash");

            migrationBuilder.CreateIndex(
                name: "IX_kiosk_tickets_IssuedAt",
                table: "kiosk_tickets",
                column: "IssuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_lpr_read_events_ParkingSessionId",
                table: "lpr_read_events",
                column: "ParkingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_lpr_read_events_ReadAt",
                table: "lpr_read_events",
                column: "ReadAt");

            migrationBuilder.CreateIndex(
                name: "IX_lpr_read_events_RegisteredVehicleId",
                table: "lpr_read_events",
                column: "RegisteredVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_registered_vehicles_EmployeeId",
                table: "registered_vehicles",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_registered_vehicles_PatientId",
                table: "registered_vehicles",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_registered_vehicles_Plate",
                table: "registered_vehicles",
                column: "Plate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_control_records");

            migrationBuilder.DropTable(
                name: "access_credentials");

            migrationBuilder.DropTable(
                name: "facial_biometric_templates");

            migrationBuilder.DropTable(
                name: "kiosk_tickets");

            migrationBuilder.DropTable(
                name: "lpr_read_events");

            migrationBuilder.DropTable(
                name: "access_turnstiles");

            migrationBuilder.DropTable(
                name: "registered_vehicles");

            migrationBuilder.DropTable(
                name: "access_zones");
        }
    }
}
