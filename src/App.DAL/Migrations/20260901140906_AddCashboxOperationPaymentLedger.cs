using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCashboxOperationPaymentLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentId",
                table: "cashbox_operations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cashbox_operations_PaymentId",
                table: "cashbox_operations",
                column: "PaymentId");

            // ────────────────────────────────────────────────────────────────────────
            // L1 BACKFILL — kassa balansının mənbəyi ödəniş sətirlərindən jurnala keçir.
            //
            // Hər ödənilmiş sətir üçün BİR mədaxil sətri yazılır: məbləğ = PaidAmount,
            // kassa = sətrin indiki CashboxId, tarix = PaymentDate.
            // Bu, BİLƏRƏKDƏN köhnə (kumulyativ) bölgünü təkrarlayır — yəni miqrasiyadan
            // sonra BÜTÜN BALANSLAR EYNİ QALIR, heç bir rəqəm sıçramır. Düzgün bölgü
            // yalnız BUNDAN SONRAKI ödənişlərdə olur, çünki köhnə sətirlərdə əvvəlki
            // ödənişin kassası heç yerdə saxlanılmayıb — o məlumat itib.
            //
            // CashboxId boş olan sətirlər ATLANIR: onların pulu heç bir kassaya
            // daxil olmayıb (BulkMarkAsPaidAsync kassa seçmir), köhnə düstur da onları
            // heç bir kassaya saymırdı.
            //
            // NOT EXISTS şərti miqrasiyanı təkrar işlədiləsi hala qarşı qoruyur.
            // ────────────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                INSERT INTO cashbox_operations
                    (CashboxId, PaymentId, Type, Amount, OperationDate, Note, CreatedAt, UpdatedAt, IsDeleted)
                SELECT p.CashboxId,
                       p.Id,
                       'Income',
                       p.PaidAmount,
                       COALESCE(p.PaymentDate, p.CreatedAt),
                       CONCAT('Miqrasiya: odenis #', p.Id, ' (', LPAD(p.Month, 2, '0'), '.', p.Year, ')'),
                       NOW(),
                       NOW(),
                       0
                FROM payments p
                WHERE p.IsDeleted = 0
                  AND p.PaidAmount > 0
                  AND p.CashboxId IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM cashbox_operations o WHERE o.PaymentId = p.Id
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Backfill-in yaratdığı sətirləri geri al — sütunu atmazdan ƏVVƏL,
            // çünki tanınma açarı məhz PaymentId-dir. Əl ilə yazılan mədaxil/məxaric
            // və köçürmə sətirlərində PaymentId boşdur, onlara toxunulmur.
            migrationBuilder.Sql("DELETE FROM cashbox_operations WHERE PaymentId IS NOT NULL;");

            migrationBuilder.DropIndex(
                name: "IX_cashbox_operations_PaymentId",
                table: "cashbox_operations");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "cashbox_operations");
        }
    }
}
