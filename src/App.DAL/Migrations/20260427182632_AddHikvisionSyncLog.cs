using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddHikvisionSyncLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_children_DiscountPercentage",
                table: "children");

            migrationBuilder.CreateTable(
                name: "HikvisionSyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SyncDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SyncTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SyncedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    IsManual = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TriggeredBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HikvisionSyncLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_children_DiscountPercentage",
                table: "children",
                sql: "`DiscountPercentage` IS NULL OR (`DiscountPercentage` >= 0 AND `DiscountPercentage` <= 100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HikvisionSyncLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_children_DiscountPercentage",
                table: "children");

            migrationBuilder.AddCheckConstraint(
                name: "CK_children_DiscountPercentage",
                table: "children",
                sql: "[DiscountPercentage] IS NULL OR ([DiscountPercentage] >= 0 AND [DiscountPercentage] <= 100)");
        }
    }
}
