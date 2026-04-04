using App.Core.Entities.Commons;
using App.Core.Enums;

namespace App.Core.Entities
{
    public class Payment : BaseEntity
    {
        public int ChildId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal OriginalAmount { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.None;
        public decimal DiscountValue { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public int? CashboxId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Debt;
        public string? Notes { get; set; }

        /// <summary>Audit sahəsi — FK deyil, yalnız user ID saxlanılır</summary>
        public string? RecordedById { get; set; }

        public Child Child { get; set; } = null!;
        public Cashbox? Cashbox { get; set; }
    }
}
