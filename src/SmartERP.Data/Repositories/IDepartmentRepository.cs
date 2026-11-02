using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IDepartmentRepository
{
    Task<IEnumerable<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(double id);
    Task CreateAsync(Department department);
    Task UpdateAsync(Department department);
    Task<string?> DeleteAsync(double id);

    // Every department ordered depth-first (parents before their reports),
    // with open headcount for the node and for its whole subtree.
    Task<IEnumerable<DepartmentNode>> GetHierarchyAsync();

    // Departments allowed as a parent. Passing an id excludes that department
    // and everything below it, so the update form cannot offer a cycle.
    Task<IEnumerable<Department>> GetParentCandidatesAsync(double? excludeDepartmentId);

    // True when parentDepartmentId is the department itself or sits below it,
    // i.e. saving the pair would create a reporting loop.
    Task<bool> WouldCreateCycleAsync(double departmentId, double parentDepartmentId);
}
