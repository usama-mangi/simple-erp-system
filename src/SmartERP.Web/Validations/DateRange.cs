using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Validations;

public class DateRangeInPast(string? _minDate) : ValidationAttribute
{
    private readonly DateTime now = DateTime.Now;
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime date)
        {
            if (_minDate != null && date < DateTime.Parse(_minDate))
                return new ValidationResult(
                    ErrorMessage ?? $"The field must be between {_minDate} and {now}"
                );

            if (date > now)
                return new ValidationResult(ErrorMessage ?? $"The field must be in the past");

            return ValidationResult.Success;
        }

        return new ValidationResult("Invalid date format");
    }
}