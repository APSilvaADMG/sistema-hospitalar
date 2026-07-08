using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppConnectHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "connect_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SentByUserId",
                table: "connect_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConsentAt",
                table: "connect_conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppOptOut",
                table: "connect_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppOptOutAt",
                table: "connect_conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_connect_messages_ExternalId",
                table: "connect_messages",
                column: "ExternalId",
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_connect_messages_SentByUserId",
                table: "connect_messages",
                column: "SentByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_connect_messages_users_SentByUserId",
                table: "connect_messages",
                column: "SentByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_connect_messages_users_SentByUserId",
                table: "connect_messages");

            migrationBuilder.DropIndex(
                name: "IX_connect_messages_ExternalId",
                table: "connect_messages");

            migrationBuilder.DropIndex(
                name: "IX_connect_messages_SentByUserId",
                table: "connect_messages");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "connect_messages");

            migrationBuilder.DropColumn(
                name: "SentByUserId",
                table: "connect_messages");

            migrationBuilder.DropColumn(
                name: "WhatsAppConsentAt",
                table: "connect_conversations");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptOut",
                table: "connect_conversations");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptOutAt",
                table: "connect_conversations");
        }
    }
}
