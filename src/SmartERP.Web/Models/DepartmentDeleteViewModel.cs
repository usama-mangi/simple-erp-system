using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Models;

public class DepartmentDeleteViewModel
{
    [Required(ErrorMessage = "Invalid department provided.")]
    public decimal Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? ParentName { get; set; }

    public string Location { get; set; } = string.Empty;

    public DateTime Established { get; set; }

    // Frozen at load time from HasDependenciesAsync. When true the delete
    // cannot succeed: dependent rows (employment assignments, management
    // history, sub-departments) would block the FK delete.
    public bool HasDependencies { get; set; }
}
