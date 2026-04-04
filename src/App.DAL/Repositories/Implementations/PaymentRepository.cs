using App.Core.Entities;
using App.Core.Enums;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Repositories.Abstractions
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Payment>> GetPaymentsByChildAsync(int childId)
        {
            return await DbSet
                .Where(p => p.ChildId == childId)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .Include(p => p.Child)
                .Include(p => p.Cashbox)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetDebtorsAsync()
        {
            return await DbSet
                .Where(p => p.Status == PaymentStatus.Debt || p.Status == PaymentStatus.PartiallyPaid)
                .Include(p => p.Child)
                    .ThenInclude(c => c.Group)
                        .ThenInclude(g => g.Division)
                .Include(p => p.Cashbox)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetMonthlyPaymentsAsync(int month, int year)
        {
            return await DbSet
                .Where(p => p.Month == month && p.Year == year)
                .Include(p => p.Child)
                    .ThenInclude(c => c.Group)
                .Include(p => p.Cashbox)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetDailyCollectionAsync(DateOnly date)
        {
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            var nextDay = dateTime.AddDays(1);
            return await DbSet
                .Where(p => p.PaymentDate >= dateTime && p.PaymentDate < nextDay)
                .Include(p => p.Child)
                .Include(p => p.Cashbox)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByGroupAsync(int groupId, int month, int year)
        {
            return await DbSet
                .Where(p => p.Month == month && p.Year == year && p.Child.GroupId == groupId)
                .Include(p => p.Child)
                .Include(p => p.Cashbox)
                .ToListAsync();
        }

        public async Task<bool> PaymentExistsForMonthAsync(int childId, int month, int year)
        {
            return await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(p => p.ChildId == childId && p.Month == month && p.Year == year);
        }

        public async Task<(IEnumerable<Payment> Items, int TotalCount)> GetFilteredAsync(
            int? childId, int? groupId, int? divisionId,
            PaymentStatus? status, int? month, int? year,
            int page, int pageSize)
        {
            var query = DbSet
                .Include(p => p.Child)
                    .ThenInclude(c => c.Group)
                        .ThenInclude(g => g.Division)
                .Include(p => p.Cashbox)
                .AsQueryable();

            if (childId.HasValue)
                query = query.Where(p => p.ChildId == childId.Value);

            if (groupId.HasValue)
                query = query.Where(p => p.Child.GroupId == groupId.Value);

            if (divisionId.HasValue)
                query = query.Where(p => p.Child.Group.DivisionId == divisionId.Value);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (month.HasValue)
                query = query.Where(p => p.Month == month.Value);

            if (year.HasValue)
                query = query.Where(p => p.Year == year.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ThenBy(p => p.Child.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
