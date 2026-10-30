using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SmartERP.Web.Validations;

public partial class ValidPhone : ValidationAttribute
{
    [GeneratedRegex(@"^([+]?[0-9]{1,3})?[. -]?(?:[(]?[0-9]{1,3}[)]?)[. -]?[0-9]{3}[. -]?[0-9]{4}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string phone && PhoneRegex().IsMatch(phone))
            return ValidationResult.Success;

        return new ValidationResult("Please provide a valid phone number.");
    }
}