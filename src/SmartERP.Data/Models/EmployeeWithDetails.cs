namespace SmartERP.Data.Models;

public class EmployeeWithDetails
{
    public double Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public double GenderId { get; set; }
    public string Address { get; set; } = string.Empty;
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