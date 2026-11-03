namespace SmartERP.Data.Models;

// An applicant candidate (separate from employee until hired).
public class Candidate
{
    public decimal Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? ResumeUrl { get; set; }
    public string Source { get; set; } = string.Empty;
    public decimal StatusId { get; set; }
    public string? StatusTitle { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
