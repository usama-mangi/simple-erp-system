using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace SmartERP.Web.Validations;

public class ValidEmail : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string email && MailAddress.TryCreate(email, out _))
            return ValidationResult.Success;


        return new ValidationResult("Invalid email provided.");
    }
}