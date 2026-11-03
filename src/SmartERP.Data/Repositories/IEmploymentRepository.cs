using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IEmploymentRepository
{
    Task<IEnumerable<EmploymentDetail>> GetByEmployeeAsync(decimal employeeId);
    Task<EmploymentDetail?> GetCurrentAsync(decimal employeeId);
    Task<EmploymentDetail?> GetByIdAsync(decimal id);
    Task CreateAsync(EmploymentDetail detail);
    Task CreateReplacingCurrentAsync(EmploymentDetail detail);
    Task EndCurrentAsync(decimal employeeId, DateOnly endDate);
    Task TransferCurrentDepartmentAsync(decimal employeeId, decimal departmentId);
    Task<IEnumerable<StatusEvent>> GetStatusEventsAsync(decimal employeeId);
}
