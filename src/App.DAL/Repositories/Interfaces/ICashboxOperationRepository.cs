using App.Core.Entities;

namespace App.DAL.Repositories.Interfaces
{
    public interface ICashboxOperationRepository : IRepository<CashboxOperation>
    {
        Task<IEnumerable<CashboxOperation>> GetByCashboxAsync(int cashboxId);
        Task<IEnumerable<CashboxOperation>> GetByCashboxAndMonthAsync(int cashboxId, int month, int year);
    }
}
