namespace SmartERP.Data.Models;

// A manager's review of an employee within a review cycle.
public class ManagerReview
{
    public decimal Id { get; set; }
    public decimal EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public decimal ReviewCycleId { get; set; }
    public string ReviewCycleTitle { get; set; } = string.Empty;
    public decimal Rating { get; set; } // 1.0 to 5.0 in 0.5 increments
    public string Comments { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}