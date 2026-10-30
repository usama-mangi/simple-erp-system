using System.ComponentModel.DataAnnotations;
using SmartERP.Web.Validations;

namespace SmartERP.Web.Models;

public class EmploymentDetailCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Invalid employee provided.")]
    public double EmployeeId { get; set; }

    [Display(Name = "Job title")]
    [Required(ErrorMessage = "Job title is required.")]
    [MaxLength(50, ErrorMessage = "Job title must be 50 or less characters.")]
    [MinLength(2, ErrorMessage = "Minimum 2 characters required.")]
    public string JobTitle { get; set; } = string.Empty;

    [Display(Name = "Salary")]
    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Salary cannot be negative.")]
    public double Salary { get; set; }

    [Display(Name = "Start date")]
    [Required(ErrorMessage = "Please provide the start date.")]
    [DateRangeInPast(null, ErrorMessage = "Start date must be today or a past date.")]
    public DateOnly StartDate { get; set; }

    [Display(Name = "End date (history backfill, optional)")]
    [DateRangeInPast(null, ErrorMessage = "End date must be today or a past date.")]
    public DateOnly? EndDate { get; set; }

    [Display(Name = "Department")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a department.")]
    public double DepartmentId { get; set; }

    [Display(Name = "Employment type")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a valid employment type.")]
    public double EmploymentTypeId { get; set; }

    [Display(Name = "Employment status")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a valid employment status.")]
    public double EmploymentStatusId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Salary <= 0)
            yield return new ValidationResult("Salary is required.", [nameof(Salary)]);

        if (StartDate == default)
            yield return new ValidationResult("Start date is required.", [nameof(StartDate)]);

        if (EndDate is { } endDate && endDate <= StartDate)
            yield return new ValidationResult(
                "End date must be after the start date.",
                [nameof(EndDate)]
            );
    }
}
