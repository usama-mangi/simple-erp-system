using System.ComponentModel.DataAnnotations;
using SmartERP.Web.Validations;

namespace SmartERP.Web.Models;

// Employee-scoped close: the employee's open assignment is ended with the
// chosen date; closed history rows are never rewritten.
public class EmploymentDetailCloseViewModel
{
    [Required(ErrorMessage = "Invalid employee provided.")]
    public double EmployeeId { get; set; }

    [Required(ErrorMessage = "Please provide the end date.")]
    [DateRangeInPast(null, ErrorMessage = "End date must be today or a past date.")]
    public DateOnly EndDate { get; set; }
}
