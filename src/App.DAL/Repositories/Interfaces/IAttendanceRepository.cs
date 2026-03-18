using App.Core.Entities;

namespace App.DAL.Repositories.Interfaces
{
    public interface IAttendanceRepository : IRepository<Attendance>
    {
        Task<IEnumerable<Attendance>> GetDailyAttendanceAsync(DateOnly date);
        Task<IEnumerable<Attendance>> GetMonthlyAttendanceAsync(int month, int year);
        Task<IEnumerable<Attendance>> GetChildAttendanceAsync(int childId, DateOnly from, DateOnly to);
        Task<IEnumerable<Attendance>> GetGroupAttendanceAsync(int groupId, DateOnly date);
    }
}
