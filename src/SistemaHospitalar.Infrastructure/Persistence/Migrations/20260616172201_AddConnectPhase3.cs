using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectPhase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "internal_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "internal_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SusGuideId",
                table: "internal_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TissGuideId",
                table: "internal_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSlaAlertAt",
                table: "connect_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "connect_calendar_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Local = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OrganizadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    AllDay = table.Column<bool>(type: "boolean", nullable: false),
                    SetorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_calendar_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_connect_calendar_events_departments_SetorId",
                        column: x => x.SetorId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_connect_calendar_events_users_OrganizadorId",
                        column: x => x.OrganizadorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "connect_context_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChatRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContextType = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_context_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_connect_context_links_chat_rooms_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalTable: "chat_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_connect_context_links_connect_tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "connect_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_connect_context_links_internal_messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "internal_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "connect_calendar_participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Response = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connect_calendar_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_connect_calendar_participants_connect_calendar_events_Event~",
                        column: x => x.EventId,
                        principalTable: "connect_calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_connect_calendar_participants_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_internal_messages_AppointmentId",
                table: "internal_messages",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_internal_messages_PatientId_CreatedAt",
                table: "internal_messages",
                columns: new[] { "PatientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_internal_messages_SusGuideId_CreatedAt",
                table: "internal_messages",
                columns: new[] { "SusGuideId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_internal_messages_TissGuideId_CreatedAt",
                table: "internal_messages",
                columns: new[] { "TissGuideId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_connect_calendar_events_Inicio_Fim",
                table: "connect_calendar_events",
                columns: new[] { "Inicio", "Fim" });

            migrationBuilder.CreateIndex(
                name: "IX_connect_calendar_events_OrganizadorId_Inicio",
                table: "connect_calendar_events",
                columns: new[] { "OrganizadorId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_connect_calendar_events_SetorId_Inicio",
                table: "connect_calendar_events",
                columns: new[] { "SetorId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_connect_calendar_participants_EventId_UserId",
                table: "connect_calendar_participants",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_connect_calendar_participants_UserId_EventId",
                table: "connect_calendar_participants",
                columns: new[] { "UserId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_connect_context_links_ChatRoomId",
                table: "connect_context_links",
                column: "ChatRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_context_links_ContextType_ContextId_CreatedAt",
                table: "connect_context_links",
                columns: new[] { "ContextType", "ContextId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_connect_context_links_MessageId",
                table: "connect_context_links",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_connect_context_links_TicketId",
                table: "connect_context_links",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_internal_messages_appointments_AppointmentId",
                table: "internal_messages",
                column: "AppointmentId",
                principalTable: "appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_internal_messages_patients_PatientId",
                table: "internal_messages",
                column: "PatientId",
                principalTable: "patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_internal_messages_sus_guides_SusGuideId",
                table: "internal_messages",
                column: "SusGuideId",
                principalTable: "sus_guides",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_internal_messages_tiss_guides_TissGuideId",
                table: "internal_messages",
                column: "TissGuideId",
                principalTable: "tiss_guides",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_internal_messages_appointments_AppointmentId",
                table: "internal_messages");

            migrationBuilder.DropForeignKey(
                name: "FK_internal_messages_patients_PatientId",
                table: "internal_messages");

            migrationBuilder.DropForeignKey(
                name: "FK_internal_messages_sus_guides_SusGuideId",
                table: "internal_messages");

            migrationBuilder.DropForeignKey(
                name: "FK_internal_messages_tiss_guides_TissGuideId",
                table: "internal_messages");

            migrationBuilder.DropTable(
                name: "connect_calendar_participants");

            migrationBuilder.DropTable(
                name: "connect_context_links");

            migrationBuilder.DropTable(
                name: "connect_calendar_events");

            migrationBuilder.DropIndex(
                name: "IX_internal_messages_AppointmentId",
                table: "internal_messages");

            migrationBuilder.DropIndex(
                name: "IX_internal_messages_PatientId_CreatedAt",
                table: "internal_messages");

            migrationBuilder.DropIndex(
                name: "IX_internal_messages_SusGuideId_CreatedAt",
                table: "internal_messages");

            migrationBuilder.DropIndex(
                name: "IX_internal_messages_TissGuideId_CreatedAt",
                table: "internal_messages");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "internal_messages");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "internal_messages");

            migrationBuilder.DropColumn(
                name: "SusGuideId",
                table: "internal_messages");

            migrationBuilder.DropColumn(
                name: "TissGuideId",
                table: "internal_messages");

            migrationBuilder.DropColumn(
                name: "LastSlaAlertAt",
                table: "connect_tickets");
        }
    }
}
