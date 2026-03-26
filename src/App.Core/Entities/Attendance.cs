using App.Core.Entities.Commons;
using App.Core.Entities.Identity;
using App.Core.Enums;

namespace App.Core.Entities
{
    public class Attendance : BaseEntity
    {
        public int ChildId { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly? ArrivalTime { get; set; }
        public TimeOnly? DepartureTime { get; set; }
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
        public bool IsLate { get; set; }
        public bool IsEarlyLeave { get; set; }
        public string? Notes { get; set; }
        public string RecordedById { get; set; } = string.Empty;

        public Child Child { get; set; } = null!;
        public User RecordedBy { get; set; } = null!;
    }
}
