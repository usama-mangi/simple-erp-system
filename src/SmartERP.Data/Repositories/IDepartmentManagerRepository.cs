using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IDepartmentManagerRepository
{
    Task<DepartmentManager?> GetCurrentByDepartmentAsync(decimal departmentId);
    Task<IEnumerable<DepartmentManager>> GetHistoryAsync(decimal departmentId);
    Task<DepartmentManager?> GetByIdAsync(decimal id);
    Task AssignAsync(DepartmentManager assignment);
    Task EndCurrentAsync(decimal departmentId, DateOnly endDate);
}
