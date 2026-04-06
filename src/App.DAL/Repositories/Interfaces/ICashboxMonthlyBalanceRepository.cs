using App.Core.Entities;

namespace App.DAL.Repositories.Interfaces
{
    public interface ICashboxMonthlyBalanceRepository : IRepository<CashboxMonthlyBalance>
    {
        Task<CashboxMonthlyBalance?> GetAsync(int cashboxId, int month, int year);
        Task<IEnumerable<CashboxMonthlyBalance>> GetByCashboxAsync(int cashboxId);
    }
}
