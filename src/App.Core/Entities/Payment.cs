using App.Core.Entities.Commons;
using App.Core.Entities.Identity;
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
        public DateTime? PaymentDate { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Debt;
        public string? Notes { get; set; }
        public string RecordedById { get; set; } = string.Empty;

        public Child Child { get; set; } = null!;
        public User RecordedBy { get; set; } = null!;
    }
}
