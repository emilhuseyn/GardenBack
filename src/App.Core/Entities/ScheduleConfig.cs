using App.Core.Entities.Commons;
using App.Core.Entities.Identity;
using App.Core.Enums;

namespace App.Core.Entities
{
    public class ScheduleConfig : BaseEntity
    {
        public ScheduleType ScheduleType { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string UpdatedById { get; set; } = string.Empty;

        public User UpdatedBy { get; set; } = null!;
    }
}
