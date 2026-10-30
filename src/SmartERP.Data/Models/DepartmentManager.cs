namespace SmartERP.Data.Models;

// One department_managers row: a manager heading a department between
// StartDate and EndDate. Closed rows are history.
public class DepartmentManager
{
    public double Id { get; set; }
    public double DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public double ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = default;
}
