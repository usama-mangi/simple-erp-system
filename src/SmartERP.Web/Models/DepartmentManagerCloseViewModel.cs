using System.ComponentModel.DataAnnotations;
using SmartERP.Web.Validations;

namespace SmartERP.Web.Models;

// Department-scoped close: the department's open manager assignment is
// ended with the chosen date; closed history rows are never rewritten.
public class DepartmentManagerCloseViewModel
{
    [Required(ErrorMessage = "Invalid department provided.")]
    public double DepartmentId { get; set; }

    [Required(ErrorMessage = "Please provide the end date.")]
    [DateRangeInPast(null, ErrorMessage = "End date must be today or a past date.")]
    public DateOnly EndDate { get; set; }
}
