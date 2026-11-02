namespace SmartERP.Data.Models;

public class AuditEntry
{
    public double Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string PkValue { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}