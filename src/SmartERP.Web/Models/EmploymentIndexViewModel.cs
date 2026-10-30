using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

// Page model for Views/Employment/Index: header employee + their detail rows.
public class EmploymentIndexViewModel
{
    public EmployeeWithDetails Employee { get; set; } = new();
    public IEnumerable<EmploymentDetail> Details { get; set; } = Enumerable.Empty<EmploymentDetail>();
}
