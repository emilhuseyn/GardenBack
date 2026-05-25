using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Repositories.Abstractions
{
    public class ScheduleConfigRepository : Repository<ScheduleConfig>, IScheduleConfigRepository
    {
        public ScheduleConfigRepository(AppDbContext context) : base(context) { }

        public async Task<ScheduleConfig?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            return await DbSet.FirstOrDefaultAsync(s => s.Code == code);
        }
    }
}
