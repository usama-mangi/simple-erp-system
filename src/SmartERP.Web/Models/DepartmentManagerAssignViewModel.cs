using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Models;

// Inline "assign manager" form on the department details page: one submit
// creates the assignment effective today, ending any current assignment
// (sp_assign_manager owns the end-then-insert pair).
public class DepartmentManagerAssignViewModel
{
    [Required(ErrorMessage = "Invalid department provided.")]
    public decimal DepartmentId { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Please choose a manager.")]
    public decimal ManagerId { get; set; }
}
