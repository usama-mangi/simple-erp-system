using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<EmployeeWithDetails?> GetByIdAsync(decimal id);
    Task CreateAsync(Employee employee);
    Task CreateWithDetailsAsync(EmployeeWithDetails employee);
    Task UpdateAsync(EmployeeWithDetails employee);
    Task<bool> HasDependenciesAsync(decimal id);
    Task DeleteAsync(decimal id);
    Task DeleteWithDetailsAsync(decimal id);
}