using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface ILookupRepository
{
    Task<IEnumerable<LookupItem>> GetGendersAsync();
    Task<IEnumerable<LookupItem>> GetEmploymentTypesAsync();
    Task<IEnumerable<LookupItem>> GetEmploymentStatusesAsync();

    // Master data consumed by employment forms, not a static lookup table:
    // the position list has to track the positions table itself.
    Task<IEnumerable<LookupItem>> GetPositionsAsync();

    Task<IEnumerable<LookupItem>> GetLeaveTypesAsync();

    // Pending owns id 1 in the seed; callers look it up by title, never by
    // assuming the number.
    Task<IEnumerable<LookupItem>> GetLeaveStatusesAsync();

    Task<IEnumerable<LookupItem>> GetDocumentTypesAsync();
}
