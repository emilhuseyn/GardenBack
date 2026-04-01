using App.Core.Entities.Commons;
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

        /// <summary>Audit sahəsi — FK deyil, yalnız user ID saxlanılır</summary>
        public string? RecordedById { get; set; }

        public Child Child { get; set; } = null!;
    }
}
