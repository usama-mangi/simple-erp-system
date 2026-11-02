namespace SmartERP.Data.Models;

// The absence strip on the dashboard: days lost to approved leave in the
// current calendar month, requests still waiting on a decision, and how many
// people are out right now. All three are sysdate-relative.
public record AbsenceStats(
    int DaysLostThisMonth,
    int PendingRequests,
    int OnLeaveToday
);
