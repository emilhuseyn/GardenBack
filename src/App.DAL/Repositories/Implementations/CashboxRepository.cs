using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Repositories.Abstractions
{
    public class CashboxRepository : Repository<Cashbox>, ICashboxRepository
    {
        public CashboxRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Cashbox>> GetActiveCashboxesAsync()
        {
            return await DbSet.Where(x => x.IsActive).ToListAsync();
        }
    }
}
