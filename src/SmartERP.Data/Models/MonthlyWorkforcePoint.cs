namespace SmartERP.Data.Models;

// One month-end snapshot of the workforce, derived from employment_details
// closed rows: a job counts toward the month if it started on or before the
// month end and closed on or after it (or is still open).
public record MonthlyWorkforcePoint(DateTime Month, int Headcount, double Payroll);
