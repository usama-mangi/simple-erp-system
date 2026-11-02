namespace SmartERP.Data.Models;

// One row of the reporting tree, already ordered depth-first by the
// repository. DirectHeadcount counts open jobs in this department alone;
// RollupHeadcount counts the department plus every department below it.
public class DepartmentNode
{
    public double Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public double? ParentDepartmentId { get; set; }
    public string? ParentName { get; set; }
    public int Depth { get; set; }
    public int DirectHeadcount { get; set; }
    public int RollupHeadcount { get; set; }
}
