using App.Core.Enums;

namespace App.Business.DTOs.Children
{
    public class CreateChildRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int GroupId { get; set; }
        public ScheduleType ScheduleType { get; set; }
        public decimal MonthlyFee { get; set; }
        public string ParentFullName { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string? ParentEmail { get; set; }
        public string? FaceIdToken { get; set; }
    }

    public class UpdateChildRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? GroupId { get; set; }
        public ScheduleType? ScheduleType { get; set; }
        public decimal? MonthlyFee { get; set; }
        public string? ParentFullName { get; set; }
        public string? ParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public string? FaceIdToken { get; set; }
    }

    public class ChildResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string DivisionName { get; set; } = string.Empty;
        public string ScheduleType { get; set; } = string.Empty;
        public decimal MonthlyFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ParentFullName { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string? ParentEmail { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class ChildDetailResponse : ChildResponse
    {
        public string? FaceIdToken { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int AttendanceDays { get; set; }
        public int AbsentDays { get; set; }
        public decimal TotalDebt { get; set; }
    }

    public class ChildFilterRequest
    {
        public int? GroupId { get; set; }
        public int? DivisionId { get; set; }
        public string? Status { get; set; }
        public string? ScheduleType { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
