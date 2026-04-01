using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class PaymentDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("MySql"))
            {
                migrationBuilder.Sql("DROP PROCEDURE IF EXISTS `RecreatePaymentDayConstraint`;");
                migrationBuilder.Sql(@"
CREATE PROCEDURE `RecreatePaymentDayConstraint`()
BEGIN
    DECLARE payment_day_column VARCHAR(64);

    IF EXISTS (
        SELECT 1
        FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'children'
          AND CONSTRAINT_NAME = 'CK_children_PaymentDay'
          AND CONSTRAINT_TYPE = 'CHECK'
    ) THEN
        ALTER TABLE `children` DROP CHECK `CK_children_PaymentDay`;
    END IF;

    SET payment_day_column = NULL;

    IF EXISTS (
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'children'
          AND COLUMN_NAME = 'PaymentDay'
    ) THEN
        SET payment_day_column = 'PaymentDay';
    ELSEIF EXISTS (
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'children'
          AND COLUMN_NAME = 'payment_day'
    ) THEN
        SET payment_day_column = 'payment_day';
    ELSE
        ALTER TABLE `children` ADD COLUMN `PaymentDay` INT NOT NULL DEFAULT 1;
        SET payment_day_column = 'PaymentDay';
    END IF;

    SET @add_constraint_sql = CONCAT(
        'ALTER TABLE `children` ADD CONSTRAINT `CK_children_PaymentDay` CHECK (`',
        payment_day_column,
        '` >= 1 AND `',
        payment_day_column,
        '` <= 28)'
    );

    PREPARE stmt FROM @add_constraint_sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
END");
                migrationBuilder.Sql("CALL `RecreatePaymentDayConstraint`();");
                migrationBuilder.Sql("DROP PROCEDURE `RecreatePaymentDayConstraint`;");
            }
            else
            {
                migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_children_PaymentDay'
      AND parent_object_id = OBJECT_ID('[children]')
)
BEGIN
    ALTER TABLE [children] DROP CONSTRAINT [CK_children_PaymentDay]
END");

                migrationBuilder.AddCheckConstraint(
                    name: "CK_children_PaymentDay",
                    table: "children",
                    sql: "[PaymentDay] >= 1 AND [PaymentDay] <= 28");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("MySql"))
            {
                migrationBuilder.Sql("DROP PROCEDURE IF EXISTS `RecreatePaymentDayConstraint`;");
                migrationBuilder.Sql(@"
CREATE PROCEDURE `RecreatePaymentDayConstraint`()
BEGIN
    DECLARE payment_day_column VARCHAR(64);

    IF EXISTS (
        SELECT 1
        FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'children'
          AND CONSTRAINT_NAME = 'CK_children_PaymentDay'
          AND CONSTRAINT_TYPE = 'CHECK'
    ) THEN
        ALTER TABLE `children` DROP CHECK `CK_children_PaymentDay`;
    END IF;

    SET payment_day_column = NULL;

    IF EXISTS (
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'children'
          AND COLUMN_NAME = 'PaymentDay'
    ) THEN
        SET payment_day_column = 'PaymentDay';
    ELSEIF EXISTS (
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'children'
          AND COLUMN_NAME = 'payment_day'
    ) THEN
        SET payment_day_column = 'payment_day';
    ELSE
        ALTER TABLE `children` ADD COLUMN `PaymentDay` INT NOT NULL DEFAULT 1;
        SET payment_day_column = 'PaymentDay';
    END IF;

    SET @add_constraint_sql = CONCAT(
        'ALTER TABLE `children` ADD CONSTRAINT `CK_children_PaymentDay` CHECK (`',
        payment_day_column,
        '` >= 1 AND `',
        payment_day_column,
        '` <= 28)'
    );

    PREPARE stmt FROM @add_constraint_sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
END");
                migrationBuilder.Sql("CALL `RecreatePaymentDayConstraint`();");
                migrationBuilder.Sql("DROP PROCEDURE `RecreatePaymentDayConstraint`;");
            }
            else
            {
                migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_children_PaymentDay'
      AND parent_object_id = OBJECT_ID('[children]')
)
BEGIN
    ALTER TABLE [children] DROP CONSTRAINT [CK_children_PaymentDay]
END");

                migrationBuilder.AddCheckConstraint(
                    name: "CK_children_PaymentDay",
                    table: "children",
                    sql: "PaymentDay >= 1 AND PaymentDay <= 28");
            }
        }
    }
}
