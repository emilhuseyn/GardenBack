using App.Core.Entities;
using App.Core.Enums;

namespace App.DAL.Repositories.Interfaces
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<IEnumerable<Payment>> GetPaymentsByChildAsync(int childId);
        /// <summary>
        /// Aktiv uşaqların borcları. <paramref name="asOf"/> güzəşt qaydasının hesablandığı tarixdir
        /// (repozitoridə DateTime.Now çağırılmır — tarix həmişə xaricdən verilir).
        /// </summary>
        Task<IEnumerable<Payment>> GetDebtorsAsync(DateTime asOf);
        Task<IEnumerable<Payment>> GetInactiveDebtorsAsync();
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
