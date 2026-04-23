using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddChildDiscountPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @ck_exists = (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                    WHERE CONSTRAINT_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'children'
                      AND CONSTRAINT_NAME = 'CK_children_DiscountPercentage'
                );

                SET @sql = IF(
                    @ck_exists > 0,
                    'ALTER TABLE `children` DROP CHECK `CK_children_DiscountPercentage`;',
                    'SELECT 1;'
                );

                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @col_exists = (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'children'
                      AND COLUMN_NAME = 'DiscountPercentage'
                );

                SET @sql = IF(
                    @col_exists > 0,
                    'ALTER TABLE `children` DROP COLUMN `DiscountPercentage`;',
                    'SELECT 1;'
                );

                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "children",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_children_DiscountPercentage",
                table: "children",
                sql: "`DiscountPercentage` IS NULL OR (`DiscountPercentage` >= 0 AND `DiscountPercentage` <= 100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_children_DiscountPercentage",
                table: "children");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "children");
        }
    }
}
