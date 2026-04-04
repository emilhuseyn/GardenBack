using App.Business.DTOs.Payments;
using App.Core.Common;
using App.Core.Enums;

namespace App.Business.Services.Interfaces
{
    /// <summary>
    /// Service for payment and billing operations.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Generates monthly debt records for all active children.
        /// </summary>
        Task GenerateMonthlyDebtsAsync(int month, int year);

        /// <summary>
        /// Generates debt records for the current month. Used by Hangfire to avoid
        /// capturing DateTime.Now at job-registration time.
        /// </summary>
        Task GenerateCurrentMonthDebtsAsync();

        /// <summary>
        /// Records a payment against a child's debt.
        /// </summary>
        Task<PaymentResponse> RecordPaymentAsync(RecordPaymentRequest dto, string recordedById);

        /// <summary>
        /// Applies a discount to an existing payment.
        /// </summary>
        Task<PaymentResponse> ApplyDiscountAsync(int paymentId, DiscountRequest dto);

        /// <summary>
        /// Generates a PDF receipt for a payment.
        /// </summary>
        Task<(byte[] FileBytes, string FileName)> GeneratePaymentReceiptPdfAsync(int paymentId);

        /// <summary>
        /// Soft deletes a payment by id.
        /// </summary>
        Task DeletePaymentAsync(int paymentId);

        /// <summary>
        /// Gets all children with unpaid debts.
        /// </summary>
        Task<IEnumerable<DebtorListItem>> GetDebtorsAsync();

        /// <summary>
        /// Gets payment history for a specific child.
        /// </summary>
        Task<IEnumerable<PaymentResponse>> GetChildPaymentHistoryAsync(int childId);

        /// <summary>
        /// Gets a filtered, paged list of payments.
        /// </summary>
        Task<PagedResponse<PaymentResponse>> GetFilteredPaymentsAsync(PaymentFilterRequest filter);

        /// <summary>
        /// Gets the daily cash collection report.
        /// </summary>
        Task<DailyCashReport> GetDailyCashReportAsync(DateOnly date);

        /// <summary>
        /// Gets the monthly cash report.
        /// </summary>
        Task<MonthlyCashReport> GetMonthlyCashReportAsync(int month, int year);

        /// <summary>
        /// Gets income report for a specific group.
        /// </summary>
        Task<MonthlyCashReport> GetGroupIncomeReportAsync(int groupId, int month, int year);

        /// <summary>
        /// Gets income report for a specific division.
        /// </summary>
        Task<MonthlyCashReport> GetDivisionIncomeReportAsync(int divisionId, int month, int year);

        /// <summary>
        /// Calculates the final amount after discount.
        /// </summary>
        decimal CalculateFinalAmount(decimal original, DiscountType type, decimal value);
    }
}
