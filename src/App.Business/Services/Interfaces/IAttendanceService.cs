using App.Business.DTOs.Attendance;

namespace App.Business.Services.Interfaces
{
    /// <summary>
    /// Service for attendance tracking operations.
    /// </summary>
    public interface IAttendanceService
    {
        /// <summary>
        /// Marks attendance for a single child.
        /// </summary>
        Task<AttendanceResponse> MarkAttendanceAsync(MarkAttendanceRequest dto, string recordedById);

        /// <summary>
        /// Records arrival time for a child.
        /// </summary>
        Task<AttendanceResponse> MarkArrivalAsync(int attendanceId, TimeOnly arrivalTime);

        /// <summary>
        /// Records departure time for a child.
        /// </summary>
        Task<AttendanceResponse> MarkDepartureAsync(int attendanceId, TimeOnly departureTime);

        /// <summary>
        /// Gets the daily attendance report, optionally filtered by group.
        /// </summary>
        Task<DailyAttendanceReport> GetDailyReportAsync(DateOnly date, int? groupId);

        /// <summary>
        /// Gets the monthly attendance report, optionally filtered by group.
        /// </summary>
        Task<MonthlyAttendanceReport> GetMonthlyReportAsync(int month, int year, int? groupId);

        /// <summary>
        /// Gets attendance records for a specific child in a date range.
        /// </summary>
        Task<IEnumerable<AttendanceResponse>> GetChildAttendanceAsync(int childId, DateOnly from, DateOnly to);

        /// <summary>
        /// Auto-detects late arrivals and early leaves based on ScheduleConfig.
        /// </summary>
        Task AutoDetectLateAndEarlyLeave();
    }
}
