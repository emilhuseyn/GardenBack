using App.Core.Entities;

namespace App.DAL.Repositories.Interfaces
{
    public interface ICashboxRepository : IRepository<Cashbox>
    {
        Task<IEnumerable<Cashbox>> GetActiveCashboxesAsync();
    }
}
