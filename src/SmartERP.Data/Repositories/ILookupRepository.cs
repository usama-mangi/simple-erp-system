using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface ILookupRepository
{
    Task<IEnumerable<LookupItem>> GetGendersAsync();
    Task<IEnumerable<LookupItem>> GetEmploymentTypesAsync();
    Task<IEnumerable<LookupItem>> GetEmploymentStatusesAsync();
}
