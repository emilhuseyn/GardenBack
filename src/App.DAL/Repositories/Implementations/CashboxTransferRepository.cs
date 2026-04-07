using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Repositories.Implementations
{
    public class CashboxTransferRepository : ICashboxTransferRepository
    {
        private readonly AppDbContext _context;

        public CashboxTransferRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(CashboxTransfer transfer)
        {
            await _context.CashboxTransfers.AddAsync(transfer);
        }

        public async Task<IEnumerable<CashboxTransfer>> GetAllAsync(int? cashboxId = null)
        {
            var query = _context.CashboxTransfers
                .Include(t => t.FromCashbox)
                .Include(t => t.ToCashbox)
                .AsQueryable();

            if (cashboxId.HasValue)
                query = query.Where(t => t.FromCashboxId == cashboxId || t.ToCashboxId == cashboxId);

            return await query.OrderByDescending(t => t.TransferDate).ToListAsync();
        }
    }
}
