using Microsoft.AspNetCore.Mvc;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

public class DashboardController(
    IStatisticsRepository _statisticsRepository,
    ILogger<DashboardController> _logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // The four reads are independent; one failure only costs the section
        // itself, so each gets its own try to build a page with everything
        // that did load. If the trend fails the KPI tiles cannot compute
        // (they are derived from it), which is when the page drops to the
        // banner-only fallback.
        try
        {
            var model = new DashboardIndexViewModel();

            model.Trend = await _statisticsRepository.GetWorkforceTrendAsync();
            PopulateKpiTiles(model);

            try
            {
                model.Summary = await _statisticsRepository.GetPeriodSummaryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load period summary for dashboard");
            }

            try
            {
                model.Payrolls = await _statisticsRepository.GetPayrollByDepartmentAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load payroll by department for dashboard");
            }

            try
            {
                model.Lists = await _statisticsRepository.GetWatchlistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load watchlists for dashboard");
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dashboard");

            TempData["ErrorMessage"] = UserMessages.GenericFailure;

            // TempData does not survive a redirect to this same failing
            // action, so the empty model renders the banner inline instead.
            return View(new DashboardIndexViewModel { DataReady = false });
        }
    }

    private static void PopulateKpiTiles(DashboardIndexViewModel model)
    {
        var points = model.Trend.Points;
        var flows = model.Trend.Flows;

        if (points.Count == 0) return;

        // Series is month-ascending ending at the current month: the last
        // point is "now", the point twelve steps back is this month a year
        // ago. With a shorter history the earliest available point stands in.
        int end = points.Count - 1;
        int yearAgoIndex = points.Count > 12 ? end - 12 : 0;

        model.CurrentHeadcount = points[end].Headcount;
        model.HeadcountYearAgo = points[yearAgoIndex].Headcount;
        model.CurrentMonthlyPayroll = points[end].Payroll;
        model.MonthlyPayrollYearAgo = points[yearAgoIndex].Payroll;

        model.HiresLast12Months = flows.Skip(Math.Max(0, flows.Count - 12)).Sum(f => f.Hires);
        model.ExitsLast12Months = flows.Skip(Math.Max(0, flows.Count - 12)).Sum(f => f.Exits);
        model.HiresPrior12Months = flows
            .Take(Math.Max(0, flows.Count - 12))
            .Skip(Math.Max(0, flows.Count - 24))
            .Sum(f => f.Hires);
        model.ExitsPrior12Months = flows
            .Take(Math.Max(0, flows.Count - 12))
            .Skip(Math.Max(0, flows.Count - 24))
            .Sum(f => f.Exits);

        model.HeadcountDelta = model.CurrentHeadcount - model.HeadcountYearAgo;
        model.HeadcountDeltaPercent = PercentDelta(model.CurrentHeadcount, model.HeadcountYearAgo);

        model.PayrollDelta = Math.Round(model.CurrentMonthlyPayroll - model.MonthlyPayrollYearAgo, 2);
        model.PayrollDeltaPercent = PercentDelta(model.CurrentMonthlyPayroll, model.MonthlyPayrollYearAgo);

        model.HireFlowDelta = model.HiresLast12Months - model.HiresPrior12Months;
        model.HireFlowDeltaPercent = PercentDelta(model.HiresLast12Months, model.HiresPrior12Months);

        model.ExitFlowDelta = model.ExitsLast12Months - model.ExitsPrior12Months;
        model.ExitFlowDeltaPercent = PercentDelta(model.ExitsLast12Months, model.ExitsPrior12Months);

        model.TrendMonths = points.Count;
    }

    private static double? PercentDelta(double current, double prior)
        => prior == 0 ? null : Math.Round((current - prior) / prior * 100, 1);
}
