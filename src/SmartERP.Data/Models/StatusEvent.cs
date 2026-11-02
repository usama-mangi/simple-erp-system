namespace SmartERP.Data.Models;

// One row per employment-status transition. Append-only: rows are inserted on
// status changes and removed only when the employee is purged.
public class StatusEvent
{
    public double Id { get; set; }
    public double EmployeeId { get; set; }
    public double? FromStatusId { get; set; }
    public string? FromStatusTitle { get; set; }
    public double ToStatusId { get; set; }
    public string ToStatusTitle { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }
}