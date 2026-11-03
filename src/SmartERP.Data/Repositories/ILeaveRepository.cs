using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// The leave aggregate: requests with their status transitions, and the
// per-employee/year balances those transitions move. Status and type ids are
// data — callers resolve them from the lookup tables and pass them in, so
// nothing here hard-codes a number.
public interface ILeaveRepository
{
    // Filtered list; a null argument means "no filter on that column".
    Task<IEnumerable<LeaveRequest>> GetRequestsAsync(decimal? employeeId, decimal? statusId);

    Task<LeaveRequest?> GetRequestByIdAsync(decimal id);

    // The single Pending row for an employee, or null when none is open.
    Task<LeaveRequest?> GetOpenRequestAsync(decimal employeeId, LeaveStatusIds statuses);

    // Pending + Approved rows: the ones that still occupy the employee's
    // calendar and therefore block an overlapping new request.
    Task<IEnumerable<LeaveRequest>> GetActiveRequestsAsync(decimal employeeId, LeaveStatusIds statuses);

    // request.StatusId must already be set to the Pending id by the caller.
    Task CreateRequestAsync(LeaveRequest request);

    // Pending -> Approved and consume the days from the balance, in one
    // transaction: a balance that cannot take the days rolls the decision back.
    Task ApproveAsync(LeaveRequest request, LeaveStatusIds statuses);

    // Pending -> Rejected. Nothing moves on the balance: nothing was consumed.
    Task RejectAsync(decimal requestId, LeaveStatusIds statuses);

    // Pending/Approved -> Cancelled. An approved cancellation gives the days
    // back; a pending one never took any.
    Task CancelAsync(LeaveRequest request, LeaveStatusIds statuses);

    Task<IEnumerable<LeaveBalance>> GetBalancesAsync(decimal employeeId, int year);

    // Entitlement only: used_days is written by ApproveAsync/CancelAsync and
    // never by the form that sets the allowance.
    Task UpsertBalanceAsync(LeaveBalance balance);
}
