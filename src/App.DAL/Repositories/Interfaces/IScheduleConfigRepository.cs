using App.Core.Entities;

namespace App.DAL.Repositories.Interfaces
{
    public interface IScheduleConfigRepository : IRepository<ScheduleConfig>
    {
        /// <summary>
        /// Code (məs. "FullDay", "HalfDay", "Evening") üzrə yalnız aktiv qrafiki tap.
        /// </summary>
        Task<ScheduleConfig?> GetByCodeAsync(string code);
    }
}
