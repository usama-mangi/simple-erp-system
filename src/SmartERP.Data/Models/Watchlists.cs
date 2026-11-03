namespace SmartERP.Data.Models;

// The "act on this today" section: people stuck in Probation, people with an
// eye on the door (On Leave / Notice Period), departments flying blind, and
// everyone whose approved leave request covers today's date.
public record Watchlists(
    IReadOnlyList<WatchlistEntry> Probation,
    IReadOnlyList<WatchlistEntry> PendingExits,
    IReadOnlyList<UnmanagedDepartment> UnmanagedDepartments,
    IReadOnlyList<WatchlistEntry> OnLeaveToday,
    IReadOnlyList<ExpiringDocument> ExpiringDocuments
);
