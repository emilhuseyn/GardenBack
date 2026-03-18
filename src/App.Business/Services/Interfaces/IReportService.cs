using App.Business.DTOs.Reports;

namespace App.Business.Services.Interfaces
{
    /// <summary>
    /// Service for generating reports and statistics.
    /// </summary>
    public interface IReportService
    {
        /// <summary>Gets overall statistics summary.</summary>
        Task<StatisticsSummaryResponse> GetStatisticsSummaryAsync();

        /// <summary>Gets per-division statistics.</summary>
        Task<IEnumerable<DivisionStatResponse>> GetDivisionStatisticsAsync();

        /// <summary>Gets active vs inactive children counts.</summary>
        Task<ActiveInactiveReport> GetActiveInactiveCountAsync();
    }
}
