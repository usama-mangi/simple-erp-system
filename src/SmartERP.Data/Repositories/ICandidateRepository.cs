using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// Applicant candidates (Choice D: many-to-many with requisitions via applications).
public interface ICandidateRepository
{
    Task<IEnumerable<Candidate>> GetAllCandidatesAsync();
    Task<IEnumerable<Candidate>> GetCandidatesByStatusAsync(decimal statusId);
    Task<Candidate?> GetCandidateByIdAsync(decimal id);
    Task<Candidate?> GetCandidateByEmailAsync(string email);
    Task CreateCandidateAsync(Candidate candidate);
    Task UpdateCandidateAsync(Candidate candidate);
}
