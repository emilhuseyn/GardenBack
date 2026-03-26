using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRecordedByForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attendances_AspNetUsers_RecordedById",
                table: "attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_AspNetUsers_RecordedById",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_RecordedById",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_attendances_RecordedById",
                table: "attendances");

            migrationBuilder.AlterColumn<string>(
                name: "RecordedById",
                table: "payments",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RecordedById",
                table: "attendances",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "payments",
                keyColumn: "RecordedById",
                keyValue: null,
                column: "RecordedById",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "RecordedById",
                table: "payments",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "attendances",
                keyColumn: "RecordedById",
                keyValue: null,
                column: "RecordedById",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "RecordedById",
                table: "attendances",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_payments_RecordedById",
                table: "payments",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_RecordedById",
                table: "attendances",
                column: "RecordedById");

            migrationBuilder.AddForeignKey(
                name: "FK_attendances_AspNetUsers_RecordedById",
                table: "attendances",
                column: "RecordedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_AspNetUsers_RecordedById",
                table: "payments",
                column: "RecordedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
