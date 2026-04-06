using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Repositories.Abstractions
{
    public class CashboxMonthlyBalanceRepository : Repository<CashboxMonthlyBalance>, ICashboxMonthlyBalanceRepository
    {
        public CashboxMonthlyBalanceRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<CashboxMonthlyBalance?> GetAsync(int cashboxId, int month, int year)
        {
            return await DbSet
                .FirstOrDefaultAsync(x => x.CashboxId == cashboxId && x.Month == month && x.Year == year);
        }

        public async Task<IEnumerable<CashboxMonthlyBalance>> GetByCashboxAsync(int cashboxId)
        {
            return await DbSet
                .Where(x => x.CashboxId == cashboxId)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();
        }
    }
}
