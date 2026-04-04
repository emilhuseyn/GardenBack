using App.Business.DTOs.Reports;
using App.Business.Services.Interfaces;
using App.Core.Enums;
using App.DAL.UnitOfWork;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Handles reports and statistics generation.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets overall statistics summary.
        /// </summary>
        public async Task<StatisticsSummaryResponse> GetStatisticsSummaryAsync()
        {
            var childList = (await _unitOfWork.Children.GetAllAsync(c => c.Status == ChildStatus.Active))
                .ToList();
            var groups = await _unitOfWork.Groups.GetGroupsWithDetailsAsync();
            var divisions = await _unitOfWork.Divisions.GetAllAsync(d => true, d => d.Groups);

            var groupToDivision = groups
                .ToDictionary(g => g.Id, g => g.Division?.Name ?? "Bölmə təyin edilməyib");

            var byDivision = childList
                .GroupBy(c => groupToDivision.TryGetValue(c.GroupId, out var divisionName)
                    ? divisionName
                    : "Bölmə təyin edilməyib")
                .Select(g => new DivisionChildCount
                {
                    DivisionName = g.Key,
                    ChildCount = g.Count()
                })
                .OrderByDescending(x => x.ChildCount)
                .ToList();

            return new StatisticsSummaryResponse
            {
                TotalActiveChildren = childList.Count,
                FullDayCount = childList.Count(c => c.ScheduleType == ScheduleType.FullDay),
                HalfDayCount = childList.Count(c => c.ScheduleType == ScheduleType.HalfDay),
                TotalGroups = groups.Count(),
                TotalDivisions = divisions.Count,
                ByDivision = byDivision
            };
        }


        /// <summary>
        /// Gets per-division statistics.
        /// </summary>
        public async Task<IEnumerable<DivisionStatResponse>> GetDivisionStatisticsAsync()
        {
            var divisions = await _unitOfWork.Divisions.GetAllAsync(d => true, d => d.Groups);
            var activeChildren = (await _unitOfWork.Children.GetActiveChildrenAsync()).ToList();

            var now = DateTime.UtcNow;
            var payments = await _unitOfWork.Payments.GetMonthlyPaymentsAsync(now.Month, now.Year);

            return divisions.Select(div =>
            {
                var divGroupIds = div.Groups.Select(g => g.Id).ToHashSet();
                return new DivisionStatResponse
                {
                    DivisionId = div.Id,
                    DivisionName = div.Name,
                    GroupCount = div.Groups.Count,
                    ChildCount = activeChildren.Count(c => divGroupIds.Contains(c.GroupId)),
                    MonthlyRevenue = payments
                        .Where(p => divGroupIds.Contains(p.Child.GroupId))
                        .Sum(p => p.PaidAmount)
                };
            });
        }

        /// <summary>
        /// Gets active vs inactive children counts.
        /// </summary>
        public async Task<ActiveInactiveReport> GetActiveInactiveCountAsync()
        {
            var active = await _unitOfWork.Children.CountAsync(c => c.Status == ChildStatus.Active);
            var inactive = await _unitOfWork.Children.CountAsync(c => c.Status == ChildStatus.Inactive);
            var total = active + inactive;

            return new ActiveInactiveReport
            {
                ActiveCount = active,
                InactiveCount = inactive,
                ActivePercentage = total > 0 ? Math.Round((decimal)active / total * 100, 2) : 0
            };
        }
    }
}
