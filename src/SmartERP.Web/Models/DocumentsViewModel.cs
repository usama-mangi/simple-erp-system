using System.ComponentModel.DataAnnotations;
using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

public class DocumentsIndexViewModel
{
    [Required]
    public decimal EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public IReadOnlyList<Document> Documents { get; set; } = [];
}

public class DocumentViewModel : IValidatableObject
{
    // 0 on create; the hidden field on edit round-trips the real id.
    public decimal Id { get; set; }

    [Required(ErrorMessage = "Invalid employee provided.")]
    public decimal EmployeeId { get; set; }

    [Required(ErrorMessage = "Please select a document type.")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Please select a document type.")]
    public decimal TypeId { get; set; }

    [Required(ErrorMessage = "Please give the document a title.")]
    [StringLength(120, ErrorMessage = "Keep the title under 120 characters.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Issued on")]
    public DateOnly? IssuedOn { get; set; }

    [Display(Name = "Expires on")]
    public DateOnly? ExpiresAt { get; set; }

    [StringLength(500, ErrorMessage = "Keep the note under 500 characters.")]
    public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IssuedOn is { } issued && ExpiresAt is { } expires && expires < issued)
            yield return new ValidationResult(
                "Expiry must be on or after the issue date.",
                [nameof(ExpiresAt)]);
    }
}
