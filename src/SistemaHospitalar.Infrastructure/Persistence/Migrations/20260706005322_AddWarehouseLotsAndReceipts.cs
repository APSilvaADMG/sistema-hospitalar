using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseLotsAndReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "estoque_entradas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReceivedByUserName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estoque_entradas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "estoque_lotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    LocationName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    StockReceiptItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estoque_lotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estoque_lotes_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "estoque_saidas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ResponsibleName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IssueType = table.Column<int>(type: "integer", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    HospitalizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estoque_saidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estoque_saidas_hospitalizations_HospitalizationId",
                        column: x => x.HospitalizationId,
                        principalTable: "hospitalizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_estoque_saidas_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "estoque_entrada_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductLotId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estoque_entrada_itens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estoque_entrada_itens_estoque_entradas_StockReceiptId",
                        column: x => x.StockReceiptId,
                        principalTable: "estoque_entradas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_estoque_entrada_itens_estoque_lotes_ProductLotId1",
                        column: x => x.ProductLotId1,
                        principalTable: "estoque_lotes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_estoque_entrada_itens_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "estoque_saida_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estoque_saida_itens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estoque_saida_itens_estoque_lotes_ProductLotId",
                        column: x => x.ProductLotId,
                        principalTable: "estoque_lotes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_estoque_saida_itens_estoque_saidas_StockIssueId",
                        column: x => x.StockIssueId,
                        principalTable: "estoque_saidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_estoque_saida_itens_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_estoque_entrada_itens_ProductId",
                table: "estoque_entrada_itens",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_entrada_itens_ProductLotId1",
                table: "estoque_entrada_itens",
                column: "ProductLotId1");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_entrada_itens_StockReceiptId",
                table: "estoque_entrada_itens",
                column: "StockReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_entradas_InvoiceNumber",
                table: "estoque_entradas",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_entradas_ReceivedAt",
                table: "estoque_entradas",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_lotes_ProductId_BatchNumber",
                table: "estoque_lotes",
                columns: new[] { "ProductId", "BatchNumber" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_saida_itens_ProductId",
                table: "estoque_saida_itens",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_saida_itens_ProductLotId",
                table: "estoque_saida_itens",
                column: "ProductLotId");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_saida_itens_StockIssueId",
                table: "estoque_saida_itens",
                column: "StockIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_saidas_CreatedAt",
                table: "estoque_saidas",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_saidas_HospitalizationId",
                table: "estoque_saidas",
                column: "HospitalizationId");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_saidas_PatientId",
                table: "estoque_saidas",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "estoque_entrada_itens");

            migrationBuilder.DropTable(
                name: "estoque_saida_itens");

            migrationBuilder.DropTable(
                name: "estoque_entradas");

            migrationBuilder.DropTable(
                name: "estoque_lotes");

            migrationBuilder.DropTable(
                name: "estoque_saidas");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "suppliers");
        }
    }
}
