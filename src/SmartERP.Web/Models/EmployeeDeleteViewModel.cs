using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Models;

public class EmployeeDeleteViewModel
{
    [Required(ErrorMessage = "Invalid employee provided.")]
    public double Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}
