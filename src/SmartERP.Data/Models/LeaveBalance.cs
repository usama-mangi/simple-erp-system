namespace SmartERP.Data.Models;

// One leave_balances row: the entitlement an employee holds for one leave
// type in one year, and how much of it the approved requests have consumed.
// used_days is written only by the approve/cancel paths, never by the
// entitlement form.
public class LeaveBalance
{
    public decimal Id { get; set; }
    public decimal EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal LeaveTypeId { get; set; }
    public string LeaveTypeTitle { get; set; } = string.Empty;
    public int Year { get; set; }
    public double EntitlementDays { get; set; }
    public double UsedDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = default;

    public double RemainingDays => EntitlementDays - UsedDays;
}
