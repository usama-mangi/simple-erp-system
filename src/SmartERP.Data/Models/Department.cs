namespace SmartERP.Data.Models;

public class Department
{
    public double Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime Established { get; set; }

    // Null means a top-level department. ParentName is read-only display data
    // joined from the parent row; writes only carry ParentDepartmentId.
    public double? ParentDepartmentId { get; set; }
    public string? ParentName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
