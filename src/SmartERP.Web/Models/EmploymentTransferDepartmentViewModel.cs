using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Models;

// Inline "change department" action on the employee details page: repoints
// the employee's open assignment without creating history.
public class EmploymentTransferDepartmentViewModel
{
    [Required(ErrorMessage = "Invalid employee provided.")]
    public double EmployeeId { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Please choose a department.")]
    public double DepartmentId { get; set; }
}
