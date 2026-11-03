using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IDepartmentRepository
{
    Task<IEnumerable<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(decimal id);
    Task CreateAsync(Department department);
    Task UpdateAsync(Department department);
    Task<string?> DeleteAsync(decimal id);

    // True when any row still references this department: job rows in
    // employment_details (open and closed), management assignments in
    // department_managers, or departments reporting to it. A hard delete
    // fails with ORA-2292 while any of these exist.
    Task<bool> HasDependenciesAsync(decimal id);

    // Every department ordered depth-first (parents before their reports),
    // with open headcount for the node and for its whole subtree.
    Task<IEnumerable<DepartmentNode>> GetHierarchyAsync();

    // Departments allowed as a parent. Passing an id excludes that department
    // and everything below it, so the update form cannot offer a cycle.
    Task<IEnumerable<Department>> GetParentCandidatesAsync(decimal? excludeDepartmentId);

    // True when parentDepartmentId is the department itself or sits below it,
    // i.e. saving the pair would create a reporting loop.
    Task<bool> WouldCreateCycleAsync(decimal departmentId, decimal parentDepartmentId);
}
