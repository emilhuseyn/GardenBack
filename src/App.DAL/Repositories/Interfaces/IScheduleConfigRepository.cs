using App.Core.Entities;
using App.Core.Enums;

namespace App.DAL.Repositories.Interfaces
{
    public interface IScheduleConfigRepository : IRepository<ScheduleConfig>
    {
        Task<ScheduleConfig?> GetByScheduleTypeAsync(ScheduleType type);
    }
}
