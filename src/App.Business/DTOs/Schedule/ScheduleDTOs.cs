using App.Core.Enums;

namespace App.Business.DTOs.Schedule
{
    public class ScheduleConfigResponse
    {
        public int Id { get; set; }
        public string ScheduleType { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateScheduleRequest
    {
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }
}
