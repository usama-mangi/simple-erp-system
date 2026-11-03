using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// The review aggregate: cycles that define appraisal windows, and manager
// reviews that sit inside those cycles. Employee and reviewer ids are data —
// callers resolve names from the employee table and pass them in.
public interface IReviewRepository
{
    // List cycles; a null active filter returns all, non-null returns active only.
    Task<IEnumerable<ReviewCycle>> GetCyclesAsync(bool? isActive = null);

    // Get a single cycle by id (or null).
    Task<ReviewCycle?> GetCycleByIdAsync(decimal id);

    // List reviews for an employee, optionally filtered by cycle.
    Task<IEnumerable<ManagerReview>> GetReviewsByEmployeeAsync(decimal employeeId, decimal? cycleId = null);

    // List all reviews for a given cycle (across all employees).
    Task<IEnumerable<ManagerReview>> GetReviewsByCycleAsync(decimal cycleId);

    // Get a single review by id (or null).
    Task<ManagerReview?> GetReviewByIdAsync(decimal id);

    // Create a new review record.
    Task CreateReviewAsync(ManagerReview review);

    // Update an existing review.
    Task UpdateReviewAsync(ManagerReview review);
}