using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// Job requisitions (new table, Choice C).
public interface IRequisitionRepository
{
    Task<IEnumerable<Requisition>> GetAllRequisitionsAsync();
    Task<IEnumerable<Requisition>> GetActiveRequisitionsAsync();
    Task<Requisition?> GetRequisitionByIdAsync(decimal id);
    Task<Requisition?> GetRequisitionByTitleAsync(string title);
    Task CreateRequisitionAsync(Requisition requisition);
    Task UpdateRequisitionAsync(Requisition requisition);
    Task DeleteRequisitionAsync(decimal id);
}
