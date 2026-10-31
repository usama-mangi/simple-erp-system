using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// Read-only statistics for the dashboard. Every method opens its own
// connection and may run several statements on it; nothing here mutates data.
public interface IStatisticsRepository
{
    // Month-end headcount/payroll and monthly hires/exits over the trend
    // window; both series are month-aligned.
    Task<WorkforceTrend> GetWorkforceTrendAsync();

    // Closed-job exits grouped by status and open-job mix by employment type.
    Task<PeriodSummary> GetPeriodSummaryAsync();

    // Open-job payroll aggregates per department, sorted by salary total.
    Task<IEnumerable<DepartmentPayroll>> GetPayrollByDepartmentAsync();

    // Probation watchlist, near-door list and managerless departments.
    Task<Watchlists> GetWatchlistsAsync();
}
