using App.Core.Entities.Commons;
using App.Core.Enums;

namespace App.Core.Entities
{
    public class CashboxOperation : BaseEntity
    {
        public int CashboxId { get; set; }
        public CashboxOperationType Type { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime OperationDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Bu əməliyyat hansı ödəniş sətrindən yaranıb. Əl ilə yazılan mədaxil/məxaric və
        /// kassalar arası köçürmə üçün BOŞ qalır.
        ///
        /// L1: kassa balansı ARTIQ ödəniş sətirlərindən hesablanmır, məhz bu jurnaldan oxunur.
        /// Səbəb: bir <see cref="Payment"/> sətri yalnız BİR kassa saxlaya bilir (sonuncunu),
        /// ona görə valideyn eyni ayı iki dəfəyə, iki fərqli kassaya ödəyəndə birinci kassa
        /// geriyə dönük pul itirirdi və mənfi balansa düşürdü. Jurnal sətri isə dəyişmir —
        /// hər ödəniş öz məbləği, öz tarixi və öz kassası ilə burada qalır.
        /// </summary>
        public int? PaymentId { get; set; }

        public Cashbox Cashbox { get; set; } = null!;
    }
}
