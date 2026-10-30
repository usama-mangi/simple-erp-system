using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

// Composed model for the employee details page: personal header plus the
// employment history partial and the department dropdown for the inline
// "change department" form.
public class EmployeeDetailsViewModel
{
    public EmployeeWithDetails Employee { get; set; } = new();
    public EmploymentIndexViewModel History { get; set; } = new();
}
