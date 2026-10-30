using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IEmploymentRepository
{
    Task<IEnumerable<EmploymentDetail>> GetByEmployeeAsync(double employeeId);
    Task<EmploymentDetail?> GetCurrentAsync(double employeeId);
    Task<EmploymentDetail?> GetByIdAsync(double id);
    Task CreateAsync(EmploymentDetail detail);
    Task CreateReplacingCurrentAsync(EmploymentDetail detail);
    Task EndCurrentAsync(double employeeId, DateOnly endDate);
    Task TransferCurrentDepartmentAsync(double employeeId, double departmentId);
}
