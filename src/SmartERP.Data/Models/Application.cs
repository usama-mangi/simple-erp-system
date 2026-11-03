namespace SmartERP.Data.Models;

// An application linking a candidate to a requisition through a pipeline stage.
public class Application
{
    public decimal Id { get; set; }
    public decimal CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public decimal RequisitionId { get; set; }
    public string RequisitionTitle { get; set; } = string.Empty;
    public decimal StageId { get; set; }
    public string StageTitle { get; set; } = string.Empty;
    public decimal StatusId { get; set; }
    public string StatusTitle { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
