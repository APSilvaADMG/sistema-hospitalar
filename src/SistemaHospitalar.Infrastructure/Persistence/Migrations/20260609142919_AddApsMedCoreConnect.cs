using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApsMedCoreConnect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "connect_checkins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedPhone = table.Column<string>(type: "text", nullable: true),
                    UpdatedAddress = table.Column<string>(type: "text", nullable: true),
                    UpdatedInsuranceNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_checkins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_connect_checkins_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_connect_checkins_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "connect_conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BotStep = table.Column<int>(type: "integer", nullable: false),
                    BotContextJson = table.Column<string>(type: "text", nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_connect_conversations_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "connect_knowledge_articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Question = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    Keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_knowledge_articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "connect_satisfaction_surveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProfessionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecialtyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_satisfaction_surveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_connect_satisfaction_surveys_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_connect_satisfaction_surveys_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_connect_satisfaction_surveys_professionals_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_connect_satisfaction_surveys_specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "specialties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "connect_scheduled_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    ReminderType = table.Column<int>(type: "integer", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_scheduled_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_connect_scheduled_messages_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_connect_scheduled_messages_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "connect_waitlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialtyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    OfferedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OfferedSlotAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OfferedProfessionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_waitlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_connect_waitlist_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_connect_waitlist_professionals_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalTable: "professionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_connect_waitlist_specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "connect_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReminderType = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_connect_messages_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_connect_messages_connect_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "connect_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_connect_checkins_AppointmentId",
                table: "connect_checkins",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_checkins_PatientId",
                table: "connect_checkins",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_conversations_Channel_ContactPhone",
                table: "connect_conversations",
                columns: new[] { "Channel", "ContactPhone" });

            migrationBuilder.CreateIndex(
                name: "IX_connect_conversations_PatientId",
                table: "connect_conversations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_messages_AppointmentId",
                table: "connect_messages",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_messages_ConversationId",
                table: "connect_messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_satisfaction_surveys_AppointmentId",
                table: "connect_satisfaction_surveys",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_satisfaction_surveys_PatientId",
                table: "connect_satisfaction_surveys",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_satisfaction_surveys_ProfessionalId",
                table: "connect_satisfaction_surveys",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_satisfaction_surveys_SpecialtyId",
                table: "connect_satisfaction_surveys",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_scheduled_messages_AppointmentId",
                table: "connect_scheduled_messages",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_scheduled_messages_IsSent_ScheduledFor",
                table: "connect_scheduled_messages",
                columns: new[] { "IsSent", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_connect_scheduled_messages_PatientId",
                table: "connect_scheduled_messages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_waitlist_PatientId",
                table: "connect_waitlist",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_waitlist_ProfessionalId",
                table: "connect_waitlist",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_waitlist_SpecialtyId",
                table: "connect_waitlist",
                column: "SpecialtyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "connect_checkins");

            migrationBuilder.DropTable(
                name: "connect_knowledge_articles");

            migrationBuilder.DropTable(
                name: "connect_messages");

            migrationBuilder.DropTable(
                name: "connect_satisfaction_surveys");

            migrationBuilder.DropTable(
                name: "connect_scheduled_messages");

            migrationBuilder.DropTable(
                name: "connect_waitlist");

            migrationBuilder.DropTable(
                name: "connect_conversations");
        }
    }
}
