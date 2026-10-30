using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

// Page model for Views/DepartmentManagers/Index: the department header
// plus its management assignments.
public class DepartmentManagerIndexViewModel
{
    public Department Department { get; set; } = new();
    public IEnumerable<DepartmentManager> Managers { get; set; } = Enumerable.Empty<DepartmentManager>();
}
