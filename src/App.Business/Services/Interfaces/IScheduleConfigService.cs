using App.Business.DTOs.Schedule;

namespace App.Business.Services.Interfaces
{
    /// <summary>
    /// Service for schedule configuration operations.
    /// </summary>
    public interface IScheduleConfigService
    {
        /// <summary>Gets all schedule configurations.</summary>
        Task<IEnumerable<ScheduleConfigResponse>> GetAllConfigsAsync();

        /// <summary>Updates a schedule configuration.</summary>
        Task<ScheduleConfigResponse> UpdateScheduleAsync(int id, UpdateScheduleRequest dto, string updatedById);
    }
}
