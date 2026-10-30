using System.ComponentModel.DataAnnotations;
using SmartERP.Web.Validations;

namespace SmartERP.Web.Models;

public class DepartmentUpdateViewModel
{
    [Required(ErrorMessage = "Invalid Department Selected")]
    public double Id { get; set; }

    [Required(ErrorMessage = "Department name is required.")]
    [MinLength(2, ErrorMessage = "Department name must be 2 or more characters.")]
    [MaxLength(50, ErrorMessage = "Department name must be 50 or less characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department Code is required.")]
    [MaxLength(3, ErrorMessage = "Department Code must be 3 or less characters long")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    [MinLength(5, ErrorMessage = "Location must be 5 or more characters.")]
    [MaxLength(100, ErrorMessage = "Location must be 100 or less characters.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "You must provide the date of establishment of a department.")]
    [DateRangeInPast(null, ErrorMessage = "Established date must be before or on today.")]
    public DateTime Established { get; set; }
}