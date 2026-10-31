namespace SmartERP.Data.Models;

// Hires and exits that happened inside one calendar month. Hires bucketed by
// start_date, exits by end_date; soft-deleted employees are excluded.
public record MonthlyFlowPoint(DateTime Month, int Hires, int Exits);
