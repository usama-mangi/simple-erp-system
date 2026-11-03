namespace SmartERP.Data.Models;

// Resolved once per action from the leave_statuses table, so no code path
// assumes an id: identity sequences restart on every rebuild and the seed
// order is what the open-request index binds to, not a constant in C#.
public record LeaveStatusIds(
    decimal Pending,
    decimal Approved,
    decimal Rejected,
    decimal Cancelled
);
