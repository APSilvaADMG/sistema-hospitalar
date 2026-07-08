using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTvCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tv_campanhas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Sector = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DailyStart = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    DailyEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    DaysOfWeek = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_campanhas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tv_campanha_midias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_campanha_midias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tv_campanha_midias_tv_campanhas_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "tv_campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tv_campanha_midias_tv_midias_MediaId",
                        column: x => x.MediaId,
                        principalTable: "tv_midias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tv_campanha_midias_CampaignId_MediaId",
                table: "tv_campanha_midias",
                columns: new[] { "CampaignId", "MediaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tv_campanha_midias_MediaId",
                table: "tv_campanha_midias",
                column: "MediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tv_campanha_midias");

            migrationBuilder.DropTable(
                name: "tv_campanhas");
        }
    }
}
