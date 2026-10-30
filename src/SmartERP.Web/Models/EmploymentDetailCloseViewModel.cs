using System.ComponentModel.DataAnnotations;
using SmartERP.Web.Validations;

namespace SmartERP.Web.Models;

public class EmploymentDetailCloseViewModel
{
    [Required(ErrorMessage = "Invalid assignment provided.")]
    public double Id { get; set; }

    [Required(ErrorMessage = "Invalid employee provided.")]
    public double EmployeeId { get; set; }

    [Required(ErrorMessage = "Please provide the end date.")]
    [DateRangeInPast(null, ErrorMessage = "End date must be today or a past date.")]
    public DateOnly EndDate { get; set; }
}
