namespace App.Business.DTOs.Attendance
{
    public class MarkAttendanceRequest
    {
        public int ChildId { get; set; }
        public DateOnly Date { get; set; }
        public bool IsPresent { get; set; }
        public TimeOnly? ArrivalTime { get; set; }
        public TimeOnly? DepartureTime { get; set; }
        public string? Notes { get; set; }
    }

    public class BulkMarkAttendanceRequest
    {
        public List<MarkAttendanceRequest> Entries { get; set; } = new();
    }

    public class AttendanceResponse
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public TimeOnly? ArrivalTime { get; set; }
        public TimeOnly? DepartureTime { get; set; }
        public bool IsPresent { get; set; }
        public bool IsLate { get; set; }
        public bool IsEarlyLeave { get; set; }
        public string? Notes { get; set; }
    }

    public class DailyAttendanceReport
    {
        public DateOnly Date { get; set; }
        public int TotalChildren { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public List<AttendanceResponse> Entries { get; set; } = new();
    }

    public class MonthlyAttendanceReport
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalWorkDays { get; set; }
        public List<ChildMonthlyAttendanceSummary> Children { get; set; } = new();
    }

    public class ChildMonthlyAttendanceSummary
    {
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int EarlyLeaveDays { get; set; }
    }
}
