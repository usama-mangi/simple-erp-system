using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

// Dashboard page model. The repositories return the trend/summary/watchlists
// aggregates; the controller derives the KPI tiles from the trend series so
// the view stays a pure renderer.
public class DashboardIndexViewModel
{
    // False when a repository read failed: the view then shows the error
    // banner and skips chart rendering instead of charting zeros.
    public bool DataReady { get; set; } = true;

    public WorkforceTrend Trend { get; set; } = new([], []);

    public PeriodSummary Summary { get; set; } = new([], []);

    public IEnumerable<DepartmentPayroll> Payrolls { get; set; } = [];

    public Watchlists Lists { get; set; } = new([], [], []);

    // The month-end window of the trend series, so the view can caption the
    // charts ("last 24 months") without guessing.
    public int TrendMonths { get; set; }

    public int CurrentHeadcount { get; set; }
    public int HeadcountYearAgo { get; set; }
    public double CurrentMonthlyPayroll { get; set; }
    public double MonthlyPayrollYearAgo { get; set; }

    public int HiresLast12Months { get; set; }
    public int HiresPrior12Months { get; set; }
    public int ExitsLast12Months { get; set; }
    public int ExitsPrior12Months { get; set; }

    // Absolute deltas for the tiles. Percent comes back null when the prior
    // period was zero (division is meaningless, and "-100%" lies about a
    // brand-new team).
    public int HeadcountDelta { get; set; }
    public double? HeadcountDeltaPercent { get; set; }
    public double PayrollDelta { get; set; }
    public double? PayrollDeltaPercent { get; set; }
    public int HireFlowDelta { get; set; }
    public double? HireFlowDeltaPercent { get; set; }
    public int ExitFlowDelta { get; set; }
    public double? ExitFlowDeltaPercent { get; set; }
}
