using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IDepartmentManagerRepository
{
    Task<DepartmentManager?> GetCurrentByDepartmentAsync(double departmentId);
    Task<IEnumerable<DepartmentManager>> GetHistoryAsync(double departmentId);
    Task<DepartmentManager?> GetByIdAsync(double id);
    Task AssignAsync(DepartmentManager assignment);
    Task EndCurrentAsync(double departmentId, DateOnly endDate);
}
