namespace SmartERP.Data.Models;

// The "act on this today" section: people stuck in Probation, people with an
// eye on the door (On Leave / Notice Period), and departments flying blind.
public record Watchlists(
    IReadOnlyList<WatchlistEntry> Probation,
    IReadOnlyList<WatchlistEntry> PendingExits,
    IReadOnlyList<UnmanagedDepartment> UnmanagedDepartments
);
