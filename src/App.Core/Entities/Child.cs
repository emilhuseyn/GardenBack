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
        /// <summary>
        /// ScheduleConfig.Code dəyəri (məs. "FullDay", "HalfDay", "Evening").
        /// Free-form string olduğu üçün admin yeni qrafiklər əlavə edə bilir.
        /// </summary>
        public string ScheduleType { get; set; } = "FullDay";
        public decimal MonthlyFee { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public int PaymentDay { get; set; } = 1;
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime? DeactivationDate { get; set; }
        public ChildStatus Status { get; set; } = ChildStatus.Active;
        public string ParentFullName { get; set; } = string.Empty;
        public string? SecondParentFullName { get; set; }
        public string ParentPhone { get; set; } = string.Empty;
        public string? SecondParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public string? FaceIdToken { get; set; }
        public int? PersonId { get; set; }

        public Group Group { get; set; } = null!;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
