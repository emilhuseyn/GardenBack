using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceStatusEnumUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPresent",
                table: "attendances");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "attendances");

            migrationBuilder.AddColumn<bool>(
                name: "IsPresent",
                table: "attendances",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
