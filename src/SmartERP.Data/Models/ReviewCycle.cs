namespace SmartERP.Data.Models;

// A review cycle (e.g. "2025 Annual Review", "Q1 2026 Review").
public class ReviewCycle
{
    public decimal Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}