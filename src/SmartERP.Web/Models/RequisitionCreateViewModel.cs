using System.ComponentModel.DataAnnotations;
using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

public class RequisitionCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Title is required.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [StringLength(500, ErrorMessage = "Description must be 500 characters or fewer.")]
    public string? Description { get; set; }

    [Display(Name = "Department")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a department.")]
    public decimal DepartmentId { get; set; }

    [Display(Name = "Position")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a position.")]
    public decimal PositionId { get; set; }

    [Display(Name = "Salary")]
    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Salary must be non-negative.")]
    public decimal Salary { get; set; }

    [Display(Name = "Openings")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Must have at least one opening.")]
    public int Openings { get; set; }

    [Display(Name = "Status")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a status.")]
    public decimal StatusId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
            yield return new ValidationResult("Title is required.", [nameof(Title)]);
        if (DepartmentId <= 0)
            yield return new ValidationResult("Please select a department.", [nameof(DepartmentId)]);
        if (PositionId <= 0)
            yield return new ValidationResult("Please select a position.", [nameof(PositionId)]);
        if (Salary < 0)
            yield return new ValidationResult("Salary must be non-negative.", [nameof(Salary)]);
        if (Openings < 1)
            yield return new ValidationResult("Must have at least one opening.", [nameof(Openings)]);
        if (StatusId <= 0)
            yield return new ValidationResult("Please select a status.", [nameof(StatusId)]);
    }
}
