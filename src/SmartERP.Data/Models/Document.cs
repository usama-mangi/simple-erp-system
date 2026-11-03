namespace SmartERP.Data.Models;

public class Document
{
    public decimal Id { get; set; }
    public decimal EmployeeId { get; set; }
    public decimal TypeId { get; set; }

    // Mapped join, not a column: shown in the vault list, never written.
    public string TypeTitle { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public DateTime? IssuedOn { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
