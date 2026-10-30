using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<EmployeeWithDetails?> GetByIdAsync(double id);
    Task CreateAsync(Employee employee);
    Task CreateWithDetailsAsync(EmployeeWithDetails employee);
    Task UpdateAsync(EmployeeWithDetails employee);
    Task DeleteAsync(double id);
}