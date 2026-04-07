using App.DAL.Presistence;
using App.DAL.Repositories.Abstractions;
using App.DAL.Repositories.Implementations;
using App.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace App.DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        private IChildRepository? _children;
        private IAttendanceRepository? _attendances;
        private IPaymentRepository? _payments;
        private ICashboxRepository? _cashboxes;
        private ICashboxOperationRepository? _cashboxOperations;
        private ICashboxMonthlyBalanceRepository? _cashboxBalances;
        private IGroupRepository? _groups;
        private IGroupTeacherRepository? _groupTeachers;
        private ICashboxTransferRepository? _cashboxTransfers;
        private IGroupLogRepository? _groupLogs;
        private IDivisionRepository? _divisions;
        private IScheduleConfigRepository? _scheduleConfigs;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public AppDbContext Context => _context;

        public IChildRepository Children =>
            _children ??= new ChildRepository(_context);

        public IAttendanceRepository Attendances =>
            _attendances ??= new AttendanceRepository(_context);

        public IPaymentRepository Payments =>
            _payments ??= new PaymentRepository(_context);

        public ICashboxRepository Cashboxes =>
            _cashboxes ??= new CashboxRepository(_context);

        public ICashboxOperationRepository CashboxOperations =>
            _cashboxOperations ??= new CashboxOperationRepository(_context);

        public ICashboxMonthlyBalanceRepository CashboxBalances =>
            _cashboxBalances ??= new CashboxMonthlyBalanceRepository(_context);

        public IGroupRepository Groups =>
            _groups ??= new GroupRepository(_context);

        public IGroupTeacherRepository GroupTeachers =>
            _groupTeachers ??= new GroupTeacherRepository(_context);

        public ICashboxTransferRepository CashboxTransfers =>
            _cashboxTransfers ??= new CashboxTransferRepository(_context);

        public IGroupLogRepository GroupLogs =>
            _groupLogs ??= new GroupLogRepository(_context);

        public IDivisionRepository Divisions =>
            _divisions ??= new DivisionRepository(_context);

        public IScheduleConfigRepository ScheduleConfigs =>
            _scheduleConfigs ??= new ScheduleConfigRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
