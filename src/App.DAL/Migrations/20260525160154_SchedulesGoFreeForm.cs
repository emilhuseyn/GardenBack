using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SchedulesGoFreeForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ═══════════════════════════════════════════════════════════════
            // 1. schedule_configs: yeni sütunları əlavə et (boş default ilə)
            // ═══════════════════════════════════════════════════════════════
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "schedule_configs",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "schedule_configs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "schedule_configs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            // ═══════════════════════════════════════════════════════════════
            // 2. schedule_configs: köhnə ScheduleType enum-undan Code və Name doldur
            //    0 = FullDay, 1 = HalfDay
            // ═══════════════════════════════════════════════════════════════
            migrationBuilder.Sql(@"
                UPDATE schedule_configs
                SET Code = CASE ScheduleType WHEN 0 THEN 'FullDay' WHEN 1 THEN 'HalfDay' ELSE CONCAT('Type_', ScheduleType) END,
                    Name = CASE ScheduleType WHEN 0 THEN 'Tam gün' WHEN 1 THEN 'Yarım gün' ELSE CONCAT('Qrafik ', ScheduleType) END
                WHERE Code = '';
            ");

            // ═══════════════════════════════════════════════════════════════
            // 3. schedule_configs: köhnə index və sütunu sil, yeni unique Code index əlavə et
            // ═══════════════════════════════════════════════════════════════
            migrationBuilder.DropIndex(
                name: "IX_schedule_configs_ScheduleType",
                table: "schedule_configs");

            migrationBuilder.DropColumn(
                name: "ScheduleType",
                table: "schedule_configs");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_configs_Code",
                table: "schedule_configs",
                column: "Code",
                unique: true);

            // ═══════════════════════════════════════════════════════════════
            // 4. children.ScheduleType: int → varchar(50). Köhnə enum dəyərlərini stringə çevir.
            //    Mətə bir ara sütun ilə backfill edib sonra dəyişdir — data itməsin.
            // ═══════════════════════════════════════════════════════════════
            migrationBuilder.AddColumn<string>(
                name: "ScheduleType_New",
                table: "children",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "FullDay")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
                UPDATE children
                SET ScheduleType_New = CASE ScheduleType WHEN 0 THEN 'FullDay' WHEN 1 THEN 'HalfDay' ELSE CONCAT('Type_', ScheduleType) END;
            ");

            migrationBuilder.DropColumn(
                name: "ScheduleType",
                table: "children");

            migrationBuilder.RenameColumn(
                name: "ScheduleType_New",
                table: "children",
                newName: "ScheduleType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri qayıtmaq — yeni-əlavə edilmiş schedule-lar üçün məlumat itə bilər
            migrationBuilder.DropIndex(
                name: "IX_schedule_configs_Code",
                table: "schedule_configs");

            migrationBuilder.AddColumn<int>(
                name: "ScheduleType",
                table: "schedule_configs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE schedule_configs
                SET ScheduleType = CASE Code WHEN 'FullDay' THEN 0 WHEN 'HalfDay' THEN 1 ELSE 0 END;
            ");

            migrationBuilder.DropColumn(name: "Code", table: "schedule_configs");
            migrationBuilder.DropColumn(name: "Name", table: "schedule_configs");
            migrationBuilder.DropColumn(name: "IsActive", table: "schedule_configs");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_configs_ScheduleType",
                table: "schedule_configs",
                column: "ScheduleType",
                unique: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleType_Old",
                table: "children",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE children
                SET ScheduleType_Old = CASE ScheduleType WHEN 'FullDay' THEN 0 WHEN 'HalfDay' THEN 1 ELSE 0 END;
            ");

            migrationBuilder.DropColumn(name: "ScheduleType", table: "children");
            migrationBuilder.RenameColumn(
                name: "ScheduleType_Old",
                table: "children",
                newName: "ScheduleType");
        }
    }
}
