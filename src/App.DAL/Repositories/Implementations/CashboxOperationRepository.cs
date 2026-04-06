using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Repositories.Abstractions
{
    public class CashboxOperationRepository : Repository<CashboxOperation>, ICashboxOperationRepository
    {
        public CashboxOperationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CashboxOperation>> GetByCashboxAsync(int cashboxId)
        {
            return await DbSet
                .Where(x => x.CashboxId == cashboxId)
                .OrderByDescending(x => x.OperationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<CashboxOperation>> GetByCashboxAndMonthAsync(int cashboxId, int month, int year)
        {
            return await DbSet
                .Where(x => x.CashboxId == cashboxId && x.OperationDate.Month == month && x.OperationDate.Year == year)
                .OrderByDescending(x => x.OperationDate)
                .ToListAsync();
        }
    }
}
