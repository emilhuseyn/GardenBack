using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;

namespace App.DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IChildRepository Children { get; }
        IAttendanceRepository Attendances { get; }
        IPaymentRepository Payments { get; }
        IGroupRepository Groups { get; }
        IDivisionRepository Divisions { get; }
        IScheduleConfigRepository ScheduleConfigs { get; }
        AppDbContext Context { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
