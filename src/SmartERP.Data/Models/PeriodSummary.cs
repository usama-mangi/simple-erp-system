namespace SmartERP.Data.Models;

// Distribution breakdowns for the reporting window: why people left and what
// the open workforce looks like by employment type.
public record PeriodSummary(
    IReadOnlyList<ExitStatusCount> ExitsByStatus,
    IReadOnlyList<EmploymentTypeShare> TypeShares
);
