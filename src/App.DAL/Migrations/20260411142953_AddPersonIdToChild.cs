using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonIdToChild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonId",
                table: "children",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_children_PersonId",
                table: "children",
                column: "PersonId",
                unique: true,
                filter: "[PersonId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_children_PersonId",
                table: "children");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "children");
        }
    }
}
