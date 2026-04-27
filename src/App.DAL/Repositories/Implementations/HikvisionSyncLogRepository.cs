using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Abstractions;
using App.DAL.Repositories.Interfaces;

namespace App.DAL.Repositories.Implementations
{
    public class HikvisionSyncLogRepository : Repository<HikvisionSyncLog>, IHikvisionSyncLogRepository
    {
        public HikvisionSyncLogRepository(AppDbContext context) : base(context) { }
    }
}
