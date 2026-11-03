using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// Applications linking candidates to requisitions through pipeline stages (Choice A: func-based unique index).
public interface IApplicationRepository
{
    Task<IEnumerable<Application>> GetApplicationsByCandidateAsync(decimal candidateId);
    Task<IEnumerable<Application>> GetApplicationsByRequisitionAsync(decimal requisitionId);
    Task<IEnumerable<Application>> GetApplicationsByStageAsync(decimal stageId);
    Task<Application?> GetApplicationByIdAsync(decimal id);
    Task<Application?> GetApplicationByCandidateAndRequisitionAsync(decimal candidateId, decimal requisitionId);
    Task CreateApplicationAsync(Application application);
    Task UpdateApplicationAsync(Application application);
}
