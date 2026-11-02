using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Models;

public class LeaveRequestCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Invalid employee provided.")]
    public double EmployeeId { get; set; }

    [Display(Name = "Leave type")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a leave type.")]
    public double LeaveTypeId { get; set; }

    // Leave is usually booked ahead, so a future start date is the normal
    // case here rather than an error to reject.
    [Display(Name = "Start date")]
    public DateOnly StartDate { get; set; }

    [Display(Name = "End date")]
    public DateOnly EndDate { get; set; }

    [Display(Name = "Reason")]
    [StringLength(200, ErrorMessage = "Keep the reason to 200 characters or fewer.")]
    public string? Reason { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // [Required] passes on a value type even when it is 0, so the
        // unselected dropdown would otherwise reach the database as id 0.
        if (EmployeeId <= 0)
            yield return new ValidationResult("Please select an employee.", [nameof(EmployeeId)]);

        if (StartDate == default)
            yield return new ValidationResult("Start date is required.", [nameof(StartDate)]);

        if (EndDate == default)
            yield return new ValidationResult("End date is required.", [nameof(EndDate)]);

        // Same-day leave is legal: chk_lr_dates allows end = start.
        if (StartDate != default && EndDate != default && EndDate < StartDate)
            yield return new ValidationResult(
                "The end date cannot be earlier than the start date.",
                [nameof(EndDate)]
            );
    }
}
