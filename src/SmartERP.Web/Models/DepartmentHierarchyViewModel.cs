using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

// Page model for Views/Department/Hierarchy: the reporting tree already
// ordered depth-first by the repository.
public class DepartmentHierarchyViewModel
{
    public IEnumerable<DepartmentNode> Nodes { get; set; } = Enumerable.Empty<DepartmentNode>();
}
