namespace SmartERP.Data.Models;

// Payroll picture of one department over its open job rows.
public record DepartmentPayroll(
    string Code,
    string Name,
    int Headcount,
    double TotalSalary,
    double AverageSalary,
    double MinimumSalary,
    double MaximumSalary
);
