using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTvSignageModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tv_avisos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Sector = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_avisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tv_clima",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TemperatureC = table.Column<decimal>(type: "numeric", nullable: false),
                    Condition = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Icon = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    HumidityPercent = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_clima", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tv_layouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ZonesJson = table.Column<string>(type: "text", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_layouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tv_midias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Sector = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_midias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tv_noticias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Sector = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_noticias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tv_displays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Sector = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Resolution = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Orientation = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PlayerToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShowPatientName = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSound = table.Column<bool>(type: "boolean", nullable: false),
                    CallDisplaySeconds = table.Column<int>(type: "integer", nullable: false),
                    WeatherCity = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_displays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tv_displays_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tv_displays_tv_layouts_LayoutId",
                        column: x => x.LayoutId,
                        principalTable: "tv_layouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tv_chamadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayId = table.Column<Guid>(type: "uuid", nullable: true),
                    TicketNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Destination = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Sector = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    KioskTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    CalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisplaySeconds = table.Column<int>(type: "integer", nullable: false),
                    ShowPatientName = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_chamadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tv_chamadas_tv_displays_DisplayId",
                        column: x => x.DisplayId,
                        principalTable: "tv_displays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tv_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tv_logs_tv_displays_DisplayId",
                        column: x => x.DisplayId,
                        principalTable: "tv_displays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tv_playlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_playlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tv_playlists_tv_displays_DisplayId",
                        column: x => x.DisplayId,
                        principalTable: "tv_displays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tv_playlist_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tv_playlist_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tv_playlist_items_tv_midias_MediaId",
                        column: x => x.MediaId,
                        principalTable: "tv_midias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tv_playlist_items_tv_playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "tv_playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tv_chamadas_CalledAt",
                table: "tv_chamadas",
                column: "CalledAt");

            migrationBuilder.CreateIndex(
                name: "IX_tv_chamadas_DisplayId",
                table: "tv_chamadas",
                column: "DisplayId");

            migrationBuilder.CreateIndex(
                name: "IX_tv_clima_City",
                table: "tv_clima",
                column: "City",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tv_displays_DepartmentId",
                table: "tv_displays",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_tv_displays_LayoutId",
                table: "tv_displays",
                column: "LayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_tv_displays_PlayerToken",
                table: "tv_displays",
                column: "PlayerToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tv_displays_Slug",
                table: "tv_displays",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tv_logs_DisplayId_OccurredAt",
                table: "tv_logs",
                columns: new[] { "DisplayId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tv_playlist_items_MediaId",
                table: "tv_playlist_items",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_tv_playlist_items_PlaylistId_MediaId",
                table: "tv_playlist_items",
                columns: new[] { "PlaylistId", "MediaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tv_playlists_DisplayId",
                table: "tv_playlists",
                column: "DisplayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tv_avisos");

            migrationBuilder.DropTable(
                name: "tv_chamadas");

            migrationBuilder.DropTable(
                name: "tv_clima");

            migrationBuilder.DropTable(
                name: "tv_logs");

            migrationBuilder.DropTable(
                name: "tv_noticias");

            migrationBuilder.DropTable(
                name: "tv_playlist_items");

            migrationBuilder.DropTable(
                name: "tv_midias");

            migrationBuilder.DropTable(
                name: "tv_playlists");

            migrationBuilder.DropTable(
                name: "tv_displays");

            migrationBuilder.DropTable(
                name: "tv_layouts");
        }
    }
}
