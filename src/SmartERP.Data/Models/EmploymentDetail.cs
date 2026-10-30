namespace SmartERP.Data.Models;

// One employment_details row. Closed rows carry an EndDate and are the history.
public class EmploymentDetail
{
    public double Id { get; set; }
    public double EmployeeId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public double Salary { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public double EmploymentTypeId { get; set; }
    public double EmploymentStatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = default;
}
