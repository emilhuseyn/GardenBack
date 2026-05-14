using App.Core.Enums;

namespace App.Business.DTOs.Payments
{
    public class PaymentFilterRequest
    {
        public int? ChildId { get; set; }
        public int? GroupId { get; set; }
        public int? DivisionId { get; set; }
        public string? Status { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class RecordPaymentRequest
    {
        public int ChildId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int CashboxId { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Optional admin courtesy reduction applied to FinalAmount at payment time
        /// (e.g. bill is 203 ₼, customer pays 200, admin forgives the 3 ₼).
        /// </summary>
        public decimal? RoundingDiscount { get; set; }
    }

    /// <summary>
    /// Bulk-pay several months at once for one child.
    /// Every selected month is marked fully paid using the child's effective fee.
    /// Per-month period overrides let admins record partial-day ranges (e.g. "1-11 avqust").
    /// </summary>
    public class RecordBulkPaymentRequest
    {
        public int ChildId { get; set; }
        public int Year { get; set; }
        public int CashboxId { get; set; }
        public List<int> Months { get; set; } = new();
        public string? Notes { get; set; }

        /// <summary>
        /// Optional: bill specific day ranges for individual months instead of the full month.
        /// Defaults stay (registration → deactivation → end-of-month) for any month not listed here.
        /// </summary>
        public List<MonthPeriodOverride>? MonthOverrides { get; set; }
    }

    public class MonthPeriodOverride
    {
        public int Month { get; set; }
        public int? StartDay { get; set; }
        public int? EndDay { get; set; }

        /// <summary>
        /// Optional admin courtesy reduction applied to this month's FinalAmount at payment time
        /// (e.g. 11-day partial bills to 284 ₼ — admin rounds down to 280 ₼, forgiving 4 ₼).
        /// </summary>
        public decimal? RoundingDiscount { get; set; }
    }

    public class RecordBulkPaymentResponse
    {
        public int PaidCount { get; set; }
        public decimal TotalPaid { get; set; }
        public List<PaymentResponse> Payments { get; set; } = new();
    }

    public class DiscountRequest
    {
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
    }

    public class PaymentResponse
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public int? CashboxId { get; set; }
        public string? CashboxName { get; set; }
        public string? CashboxType { get; set; }
        public decimal OriginalAmount { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingDebt { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class DebtorListItem
    {
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string DivisionName { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public decimal TotalDebt { get; set; }
        public List<PaymentResponse> UnpaidMonths { get; set; } = new();
    }

    public class DailyCashReport
    {
        public DateOnly Date { get; set; }
        public decimal TotalCollected { get; set; }
        public int PaymentCount { get; set; }
        public List<PaymentResponse> Payments { get; set; } = new();
    }

    public class MonthlyCashReport
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalExpected { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalDebt { get; set; }
        public int PaidCount { get; set; }
        public int PartialCount { get; set; }
        public int DebtCount { get; set; }
    }
}
