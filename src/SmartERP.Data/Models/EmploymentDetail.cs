namespace SmartERP.Data.Models;

// One employment_details row. Closed rows carry an EndDate and are the history.
public class EmploymentDetail
{
    public decimal Id { get; set; }
    public decimal EmployeeId { get; set; }
    public decimal PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public double Salary { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal EmploymentTypeId { get; set; }
    public decimal EmploymentStatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = default;
}
