using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IDepartmentRepository
{
    Task<IEnumerable<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(double id);
    Task CreateAsync(Department department);
    Task UpdateAsync(Department department);
    Task<string?> DeleteAsync(double id);
}