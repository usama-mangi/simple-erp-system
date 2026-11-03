using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IDocumentRepository
{
    // Documents for one non-deleted employee, joined with the type title,
    // soonest expiry first (never-expiring rows last).
    Task<IReadOnlyList<Document>> GetForEmployeeAsync(decimal employeeId);

    Task<Document?> GetByIdAsync(decimal id);

    Task CreateAsync(Document document);
    Task UpdateAsync(Document document);

    // Metadata only: deleting a document row has no side effects beyond the
    // row itself, so no transaction and no dependency check is needed.
    Task DeleteAsync(decimal id);
}
