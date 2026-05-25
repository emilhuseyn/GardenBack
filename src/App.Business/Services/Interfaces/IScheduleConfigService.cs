using App.Business.DTOs.Schedule;

namespace App.Business.Services.Interfaces
{
    /// <summary>
    /// Service for schedule configuration CRUD operations.
    /// </summary>
    public interface IScheduleConfigService
    {
        Task<IEnumerable<ScheduleConfigResponse>> GetAllConfigsAsync(bool includeInactive = false);
        Task<ScheduleConfigResponse> CreateScheduleAsync(CreateScheduleRequest dto, string userId);
        Task<ScheduleConfigResponse> UpdateScheduleAsync(int id, UpdateScheduleRequest dto, string userId);
        Task DeleteScheduleAsync(int id);
    }
}
