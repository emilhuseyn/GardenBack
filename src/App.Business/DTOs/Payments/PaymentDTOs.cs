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
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
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
