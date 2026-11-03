namespace SmartERP.Data.Models;

// A configurable application pipeline stage (Choice B: configurable via lookup).
public class ApplicationStage
{
    public decimal Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
