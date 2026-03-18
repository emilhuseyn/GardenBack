using App.Core.Entities.Commons;
using App.Core.Enums;

namespace App.Core.Entities
{
    public class Child : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int GroupId { get; set; }
        public ScheduleType ScheduleType { get; set; }
        public decimal MonthlyFee { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public ChildStatus Status { get; set; } = ChildStatus.Active;
        public string ParentFullName { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string? ParentEmail { get; set; }
        public string? FaceIdToken { get; set; }

        public Group Group { get; set; } = null!;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
