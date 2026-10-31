namespace SmartERP.Data.Models;

// One row of an action list: who, where, since when, and how long they have
// been sitting in their current open employment status.
public record WatchlistEntry(
    string EmployeeName,
    string JobTitle,
    string DepartmentName,
    double Salary,
    DateTime StartDate,
    int DaysInStatus
);
