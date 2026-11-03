namespace SmartERP.Data.Models;

// A job opening/requisition (Choice C: uses existing dept/position, new table for req data).
public class Requisition
{
    public decimal Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DepartmentId { get; set; }
    public string? DepartmentName { get; set; } = string.Empty;
    public decimal PositionId { get; set; }
    public string? PositionTitle { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public int Openings { get; set; }
    public decimal StatusId { get; set; }
    public string? StatusTitle { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
