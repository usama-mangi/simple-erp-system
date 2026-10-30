using System.ComponentModel.DataAnnotations;
using SmartERP.Web.Validations;

namespace SmartERP.Web.Models;

public class EmployeeCreateViewModel
{
    [Required(ErrorMessage = "First name is required.")]
    [MinLength(3, ErrorMessage = "Minimum 3 characters required.")]
    [MaxLength(50, ErrorMessage = "Maximum 50 characters allowed.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MinLength(3, ErrorMessage = "Minimum 3 characters required.")]
    [MaxLength(50, ErrorMessage = "Maximum 50 characters allowed.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide an email address.")]
    [MaxLength(100, ErrorMessage = "Email must be 100 or less characters.")]
    [ValidEmail]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide a phone number")]
    [MaxLength(20, ErrorMessage = "Phone cannot be more than 20 characters long.")]
    [ValidPhone]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide date of birth.")]
    [DateRangeInPast("1965-01-01", ErrorMessage = "Date too old. Cannot assign year older than 1965.")]
    public DateOnly DateOfBirth { get; set; }
}