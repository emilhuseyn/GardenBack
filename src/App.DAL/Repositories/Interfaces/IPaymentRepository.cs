using App.Core.Entities;
using App.Core.Enums;

namespace App.DAL.Repositories.Interfaces
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<IEnumerable<Payment>> GetPaymentsByChildAsync(int childId);
        Task<IEnumerable<Payment>> GetDebtorsAsync();
        Task<IEnumerable<Payment>> GetMonthlyPaymentsAsync(int month, int year);
        Task<IEnumerable<Payment>> GetDailyCollectionAsync(DateOnly date);
        Task<IEnumerable<Payment>> GetPaymentsByGroupAsync(int groupId, int month, int year);
        Task<bool> PaymentExistsForMonthAsync(int childId, int month, int year);
        /// <summary>
        /// Returns a filtered, paged set of payments resolved at DB level.
        /// </summary>
        Task<(IEnumerable<Payment> Items, int TotalCount)> GetFilteredAsync(
            int? childId, int? groupId, int? divisionId,
            PaymentStatus? status, int? month, int? year,
            int page, int pageSize);
    }
}
