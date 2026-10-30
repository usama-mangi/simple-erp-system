using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Validations;

public class DateRangeInPast(string? _minDate) : ValidationAttribute
{
    private readonly DateTime now = DateTime.Now;
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // View models bind both DateTime (Department established) and DateOnly
        // (Employee date of birth); normalize DateOnly before the range checks.
        DateTime? date = value switch
        {
            DateTime dt => dt,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            _ => null
        };

        if (date is null)
            return new ValidationResult("Invalid date format");

        if (_minDate != null && date < DateTime.Parse(_minDate))
            return new ValidationResult(
                ErrorMessage ?? $"The field must be between {_minDate} and {now}"
            );

        if (date > now)
            return new ValidationResult(ErrorMessage ?? $"The field must be in the past");

        return ValidationResult.Success;
    }
}