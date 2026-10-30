using System.ComponentModel.DataAnnotations;
using SmartERP.Web.Validations;

namespace SmartERP.Web.Models;

public class DepartmentManagerCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Invalid department provided.")]
    public double DepartmentId { get; set; }

    [Display(Name = "Manager")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a manager.")]
    public double ManagerId { get; set; }

    [Display(Name = "Start date")]
    [Required(ErrorMessage = "Please provide the start date.")]
    [DateRangeInPast(null, ErrorMessage = "Start date must be today or a past date.")]
    public DateOnly StartDate { get; set; }

    [Display(Name = "End date (history backfill, optional)")]
    [DateRangeInPast(null, ErrorMessage = "End date must be today or a past date.")]
    public DateOnly? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate == default)
            yield return new ValidationResult("Start date is required.", [nameof(StartDate)]);

        if (EndDate is { } endDate && endDate <= StartDate)
            yield return new ValidationResult(
                "End date must be after the start date.",
                [nameof(EndDate)]
            );
    }
}
