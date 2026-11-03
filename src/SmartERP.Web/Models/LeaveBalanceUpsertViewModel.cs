using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Models;

// Sets or revises an allowance. Entitlement only: used_days is owned by the
// approve/cancel paths, so this form can never touch it.
public class LeaveBalanceUpsertViewModel
{
    [Required(ErrorMessage = "Invalid employee provided.")]
    public decimal EmployeeId { get; set; }

    [Display(Name = "Leave type")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a leave type.")]
    public decimal LeaveTypeId { get; set; }

    [Display(Name = "Year")]
    [Range(2000, 2100, ErrorMessage = "Please provide a valid year.")]
    public int Year { get; set; }

    [Display(Name = "Entitlement (days)")]
    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Entitlement cannot be negative.")]
    public double EntitlementDays { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // [Required] passes on a 0 id: the unselected dropdown must fail here.
        if (EmployeeId <= 0)
            yield return new ValidationResult("Please select an employee.", [nameof(EmployeeId)]);

        if (EntitlementDays <= 0)
            yield return new ValidationResult(
                "Entitlement must be greater than zero days.",
                [nameof(EntitlementDays)]
            );
    }
}
