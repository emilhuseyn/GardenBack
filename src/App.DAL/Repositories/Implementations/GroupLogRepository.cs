using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;

namespace App.DAL.Repositories.Abstractions
{
    public class GroupLogRepository : Repository<GroupLog>, IGroupLogRepository
    {
        public GroupLogRepository(AppDbContext context) : base(context)
        {
        }
    }
}
