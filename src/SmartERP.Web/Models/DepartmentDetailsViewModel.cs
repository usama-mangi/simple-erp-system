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
}
