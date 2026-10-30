using System.ComponentModel.DataAnnotations;
using SmartERP.Web.Validations;

namespace SmartERP.Web.Models;

public class EmployeeUpdateViewModel
{
    [Required(ErrorMessage = "Invalid employee provided.")]
    public double Id { get; set; }

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

    [Required(ErrorMessage = "Please select a gender.")]
    [Range(1, 3, ErrorMessage = "Please select a valid gender.")]
    public double GenderId { get; set; }

    [Required(ErrorMessage = "Please provide an address.")]
    [MaxLength(100, ErrorMessage = "Address must be 100 or less characters.")]
    public string Address { get; set; } = string.Empty;

    // Frozen at load time from the current employment_details row:
    // the details section is only edited when an open assignment exists.
    [Display(Name = "Assign a job right away")]
    public bool HasEmploymentDetails { get; set; }

    [Display(Name = "Job title")]
    [MaxLength(50, ErrorMessage = "Job title must be 50 or less characters.")]
    public string JobTitle { get; set; } = string.Empty;

    [Display(Name = "Salary")]
    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Salary cannot be negative.")]
    public double Salary { get; set; }

    [Display(Name = "Start date")]
    [DateRangeInPast(null, ErrorMessage = "Start date must be today or a past date.")]
    public DateOnly StartDate { get; set; }

    [Display(Name = "Department")]
    public double DepartmentId { get; set; }

    [Display(Name = "Employment type")]
    [Range(1, 5, ErrorMessage = "Please select a valid employment type.")]
    public double EmploymentTypeId { get; set; }

    [Display(Name = "Employment status")]
    [Range(1, 8, ErrorMessage = "Please select a valid employment status.")]
    public double EmploymentStatusId { get; set; }

    // Detail blocks only validate when the employee actually has an open row;
    // without one the detail update statement has nothing to match.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!HasEmploymentDetails) yield break;

        if (string.IsNullOrWhiteSpace(JobTitle))
            yield return new ValidationResult("Job title is required.", [nameof(JobTitle)]);

        if (Salary <= 0)
            yield return new ValidationResult("Salary is required.", [nameof(Salary)]);

        if (StartDate == default)
            yield return new ValidationResult("Start date is required.", [nameof(StartDate)]);

        if (DepartmentId <= 0)
            yield return new ValidationResult("Please select a department for the job.", [nameof(DepartmentId)]);

        if (EmploymentTypeId <= 0)
            yield return new ValidationResult("Please select an employment type for the job.", [nameof(EmploymentTypeId)]);

        if (EmploymentStatusId <= 0)
            yield return new ValidationResult("Please select an employment status for the job.", [nameof(EmploymentStatusId)]);
    }
}
