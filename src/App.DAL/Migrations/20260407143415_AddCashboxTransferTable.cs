using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCashboxTransferTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashboxTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FromCashboxId = table.Column<int>(type: "int", nullable: false),
                    ToCashboxId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TransferDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashboxTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashboxTransfers_cashboxes_FromCashboxId",
                        column: x => x.FromCashboxId,
                        principalTable: "cashboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashboxTransfers_cashboxes_ToCashboxId",
                        column: x => x.ToCashboxId,
                        principalTable: "cashboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CashboxTransfers_FromCashboxId",
                table: "CashboxTransfers",
                column: "FromCashboxId");

            migrationBuilder.CreateIndex(
                name: "IX_CashboxTransfers_ToCashboxId",
                table: "CashboxTransfers",
                column: "ToCashboxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashboxTransfers");
        }
    }
}
