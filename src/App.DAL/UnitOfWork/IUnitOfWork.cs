using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;

namespace App.DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IChildRepository Children { get; }
        IAttendanceRepository Attendances { get; }
        IPaymentRepository Payments { get; }
        ICashboxRepository Cashboxes { get; }
        ICashboxOperationRepository CashboxOperations { get; }
        ICashboxMonthlyBalanceRepository CashboxBalances { get; }
        IGroupRepository Groups { get; }
        IGroupTeacherRepository GroupTeachers { get; }
        ICashboxTransferRepository CashboxTransfers { get; }
        IGroupLogRepository GroupLogs { get; }
        IDivisionRepository Divisions { get; }
        IScheduleConfigRepository ScheduleConfigs { get; }
        AppDbContext Context { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
