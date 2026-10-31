namespace SmartERP.Data.Models;

// The two time series the dashboard charts draw: month-end workforce state
// and the hires/exits ramp per month. Same window, aligned months.
public record WorkforceTrend(
    IReadOnlyList<MonthlyWorkforcePoint> Points,
    IReadOnlyList<MonthlyFlowPoint> Flows
);
