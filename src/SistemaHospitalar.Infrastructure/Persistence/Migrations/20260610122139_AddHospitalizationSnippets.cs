using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationSnippets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hospitalization_snippets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospitalization_snippets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_snippets_Type_NormalizedText",
                table: "hospitalization_snippets",
                columns: new[] { "Type", "NormalizedText" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hospitalization_snippets_Type_UsageCount",
                table: "hospitalization_snippets",
                columns: new[] { "Type", "UsageCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hospitalization_snippets");
        }
    }
}
