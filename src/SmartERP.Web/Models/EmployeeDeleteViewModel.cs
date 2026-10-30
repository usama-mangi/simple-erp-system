using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Models;

public class EmployeeDeleteViewModel
{
    [Required(ErrorMessage = "Invalid employee provided.")]
    public double Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    // Frozen at load time from HasDependenciesAsync. Drives the destructive
    // branch (DeleteWithDetailsAsync) and the extra warning UI in the view.
    public bool HasDependencies { get; set; }
}
