using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Models;

public class ReviewCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Invalid employee provided.")]
    public decimal EmployeeId { get; set; }

    [Display(Name = "Employee")]
    public string EmployeeName { get; set; } = string.Empty;

    [Display(Name = "Review cycle")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a review cycle.")]
    public decimal CycleId { get; set; }

    [Display(Name = "Cycle title")]
    public string CycleTitle { get; set; } = string.Empty;

    [Display(Name = "Reviewer")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a reviewer.")]
    public decimal ReviewerId { get; set; }

    [Display(Name = "Reviewer name")]
    public string ReviewerName { get; set; } = string.Empty;

    [Display(Name = "Rating")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public decimal Rating { get; set; }

    [Display(Name = "Comments")]
    [StringLength(500, ErrorMessage = "Keep comments to 500 characters or fewer.")]
    public string Comments { get; set; } = string.Empty;

    [Display(Name = "Review date")]
    public DateTime ReviewDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EmployeeId <= 0)
            yield return new ValidationResult("Please select an employee.", [nameof(EmployeeId)]);

        if (CycleId <= 0)
            yield return new ValidationResult("Please select a review cycle.", [nameof(CycleId)]);

        if (ReviewerId <= 0 || ReviewerId == EmployeeId)
            yield return new ValidationResult("Please select a valid reviewer (cannot be the employee).", [nameof(ReviewerId)]);

        if (Rating < 1 || Rating > 5)
            yield return new ValidationResult("Rating must be between 1 and 5.", [nameof(Rating)]);

        if (ReviewDate == default)
            yield return new ValidationResult("Review date is required.", [nameof(ReviewDate)]);
    }
}
