using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

// Balances page: one employee, one year, their entitlement rows, plus the
// form that sets or revises an allowance. Upsert carries the submitted form
// back on a failed post so the fields stay filled.
public class LeaveBalancesViewModel
{
    public EmployeeWithDetails Employee { get; set; } = new();

    public int Year { get; set; }

    public IEnumerable<LeaveBalance> Balances { get; set; } = Enumerable.Empty<LeaveBalance>();

    public LeaveBalanceUpsertViewModel Upsert { get; set; } = new();

    // Every leave type, so the table can show "Not set" for the ones with no
    // balance row yet instead of hiding them.
    public IEnumerable<LookupItem> LeaveTypes { get; set; } = Enumerable.Empty<LookupItem>();

    public IEnumerable<SelectListItem>? LeaveTypeOptions { get; set; }
}
