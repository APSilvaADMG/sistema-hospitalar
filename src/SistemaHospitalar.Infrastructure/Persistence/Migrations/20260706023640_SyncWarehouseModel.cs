using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncWarehouseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_estoque_entrada_itens_estoque_lotes_ProductLotId1",
                table: "estoque_entrada_itens");

            migrationBuilder.DropIndex(
                name: "IX_estoque_entrada_itens_ProductLotId1",
                table: "estoque_entrada_itens");

            migrationBuilder.DropColumn(
                name: "StockReceiptItemId",
                table: "estoque_lotes");

            migrationBuilder.DropColumn(
                name: "ProductLotId1",
                table: "estoque_entrada_itens");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_entrada_itens_ProductLotId",
                table: "estoque_entrada_itens",
                column: "ProductLotId");

            migrationBuilder.AddForeignKey(
                name: "FK_estoque_entrada_itens_estoque_lotes_ProductLotId",
                table: "estoque_entrada_itens",
                column: "ProductLotId",
                principalTable: "estoque_lotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_estoque_entrada_itens_estoque_lotes_ProductLotId",
                table: "estoque_entrada_itens");

            migrationBuilder.DropIndex(
                name: "IX_estoque_entrada_itens_ProductLotId",
                table: "estoque_entrada_itens");

            migrationBuilder.AddColumn<Guid>(
                name: "StockReceiptItemId",
                table: "estoque_lotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductLotId1",
                table: "estoque_entrada_itens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_estoque_entrada_itens_ProductLotId1",
                table: "estoque_entrada_itens",
                column: "ProductLotId1");

            migrationBuilder.AddForeignKey(
                name: "FK_estoque_entrada_itens_estoque_lotes_ProductLotId1",
                table: "estoque_entrada_itens",
                column: "ProductLotId1",
                principalTable: "estoque_lotes",
                principalColumn: "Id");
        }
    }
}
