using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectInboxFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "connect_conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HumanRequestedAt",
                table: "connect_conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Queue",
                table: "connect_conversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "connect_conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_connect_conversations_AssignedUserId",
                table: "connect_conversations",
                column: "AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_connect_conversations_users_AssignedUserId",
                table: "connect_conversations",
                column: "AssignedUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_connect_conversations_users_AssignedUserId",
                table: "connect_conversations");

            migrationBuilder.DropIndex(
                name: "IX_connect_conversations_AssignedUserId",
                table: "connect_conversations");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "connect_conversations");

            migrationBuilder.DropColumn(
                name: "HumanRequestedAt",
                table: "connect_conversations");

            migrationBuilder.DropColumn(
                name: "Queue",
                table: "connect_conversations");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "connect_conversations");
        }
    }
}
