namespace SmartERP.Data.Models;

// A document whose expires_at falls inside the watch window: who holds it,
// what it is, and how long is left. DaysLeft goes negative for lapsed rows
// so HR sees the lapse, not just the cliff edge.
public record ExpiringDocument(
    decimal EmployeeId,
    string EmployeeName,
    string DocumentTitle,
    string TypeTitle,
    DateTime ExpiresAt,
    int DaysLeft
);
