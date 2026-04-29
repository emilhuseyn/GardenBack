using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Abstractions;
using App.DAL.Repositories.Interfaces;

namespace App.DAL.Repositories.Implementations
{
    public class SystemSettingRepository : Repository<SystemSetting>, ISystemSettingRepository
    {
        public SystemSettingRepository(AppDbContext context) : base(context) { }
    }
}
