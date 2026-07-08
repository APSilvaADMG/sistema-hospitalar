using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficialUpdatesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_log_integracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    TriggeredBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_log_integracao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_versao_oficial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    VersionLabel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RemoteVersionLabel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InstalledFileHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RemoteFileHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InstalledRecordCount = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_versao_oficial", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_log_integracao_CreatedAt",
                table: "tb_log_integracao",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tb_log_integracao_SourceType",
                table: "tb_log_integracao",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_tb_versao_oficial_SourceType",
                table: "tb_versao_oficial",
                column: "SourceType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_log_integracao");

            migrationBuilder.DropTable(
                name: "tb_versao_oficial");
        }
    }
}
