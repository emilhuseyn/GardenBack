using App.Core.Entities;

namespace App.DAL.Repositories.Interfaces
{
    public interface ICashboxTransferRepository
    {
        Task AddAsync(CashboxTransfer transfer);
        Task<IEnumerable<CashboxTransfer>> GetAllAsync(int? cashboxId = null);
    }
}
