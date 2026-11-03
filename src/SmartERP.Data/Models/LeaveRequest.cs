namespace SmartERP.Data.Models;

// One leave_requests row. The joined titles come from the lookups so callers
// never re-query them; Days is the inclusive calendar-day count (start day
// counts, end day counts) that the balance gives up on approval.
public class LeaveRequest
{
    public decimal Id { get; set; }
    public decimal EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal LeaveTypeId { get; set; }
    public string LeaveTypeTitle { get; set; } = string.Empty;
    public decimal StatusId { get; set; }
    public string StatusTitle { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? DecidedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = default;

    // Inclusive day count: a request from Monday to Monday is 8 days, not 7.
    public int Days => EndDate.Date >= StartDate.Date
        ? (EndDate.Date - StartDate.Date).Days + 1
        : 0;
}
