using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

// Page model for the employment history partial: header employee plus
// their detail rows. DepartmentOptions feeds the inline "change department"
// form when the partial is rendered from the employee details page; plain
// history listings leave it empty.
public class EmploymentIndexViewModel
{
    public EmployeeWithDetails Employee { get; set; } = new();
    public IEnumerable<EmploymentDetail> Details { get; set; } = Enumerable.Empty<EmploymentDetail>();
    public IEnumerable<SelectListItem>? DepartmentOptions { get; set; }
}
