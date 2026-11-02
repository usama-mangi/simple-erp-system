using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

// List page: the request rows plus the two filters. EmployeeName is filled
// only when the list is scoped to a single employee; a global list gets the
// employee dropdown instead.
public class LeaveIndexViewModel
{
    public IEnumerable<LeaveRequest> Requests { get; set; } = Enumerable.Empty<LeaveRequest>();

    public double? EmployeeId { get; set; }

    public double? StatusId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public IEnumerable<SelectListItem>? EmployeeOptions { get; set; }

    public IEnumerable<SelectListItem>? StatusOptions { get; set; }

    // Resolved ids so the view can branch on a real status id instead of
    // comparing title strings.
    public LeaveStatusIds Statuses { get; set; } = new(0, 0, 0, 0);
}
