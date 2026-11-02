using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

// Composed model for the department details page: the department header
// plus its management history and the employee dropdown for the inline
// assign form.
public class DepartmentDetailsViewModel
{
    public Department Department { get; set; } = new();
    public IEnumerable<DepartmentManager> Managers { get; set; } = Enumerable.Empty<DepartmentManager>();
    public IEnumerable<SelectListItem> ManagerOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    // Direct reports by reporting line (not people).
    public IEnumerable<DepartmentNode> Children { get; set; } = Enumerable.Empty<DepartmentNode>();

    // Open jobs in this department alone, then this department plus all of
    // its descendants.
    public int DirectHeadcount { get; set; }
    public int RollupHeadcount { get; set; }
}
