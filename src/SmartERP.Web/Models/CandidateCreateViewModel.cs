using System.ComponentModel.DataAnnotations;
using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

public class CandidateCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [StringLength(20, ErrorMessage = "Phone must be 20 characters or fewer.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Resume URL")]
    public string? ResumeUrl { get; set; }

    [Display(Name = "Source")]
    public string Source { get; set; } = string.Empty;

    [Display(Name = "Status")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a status.")]
    public decimal StatusId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(FirstName))
            yield return new ValidationResult("First name is required.", [nameof(FirstName)]);
        if (string.IsNullOrWhiteSpace(LastName))
            yield return new ValidationResult("Last name is required.", [nameof(LastName)]);
        if (string.IsNullOrWhiteSpace(Email))
            yield return new ValidationResult("Email is required.", [nameof(Email)]);
        if (!string.IsNullOrWhiteSpace(Email) && !Email.Contains("@"))
            yield return new ValidationResult("Invalid email address.", [nameof(Email)]);
        if (string.IsNullOrWhiteSpace(Phone))
            yield return new ValidationResult("Phone is required.", [nameof(Phone)]);
        if (StatusId <= 0)
            yield return new ValidationResult("Please select a status.", [nameof(StatusId)]);
    }
}
