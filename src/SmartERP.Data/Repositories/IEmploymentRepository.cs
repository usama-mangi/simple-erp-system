using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IEmploymentDetailsRepository
{
    Task<IEnumerable<EmploymentDetail>> GetAllByEmployeeAsync(double employeeId);
    Task<EmploymentDetail?> GetByIdAsync(double id);
    Task CreateAsync(EmploymentDetail detail);
    Task CreateReplacingCurrentAsync(EmploymentDetail detail);
    Task CloseAsync(double id, DateOnly endDate);
}
