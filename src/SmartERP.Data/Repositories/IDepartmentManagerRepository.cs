using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IDepartmentManagersRepository
{
    Task<IEnumerable<DepartmentManager>> GetAllByDepartmentAsync(double departmentId);
    Task<DepartmentManager?> GetByIdAsync(double id);
    Task CreateAsync(DepartmentManager assignment);
    Task CloseAsync(double id, DateOnly endDate);
}
