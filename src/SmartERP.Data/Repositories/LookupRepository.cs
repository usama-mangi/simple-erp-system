using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// Lookup tables (genders, employment_types, employment_status) are the source of
// truth for their own ids: identity sequences can drift across rebuilds and
// inserts, so enum positions must never be assumed in UI bindings.
public class LookupRepository(
    IDbConnectionFactory _dbConnection,
    OracleExceptionHandler _oracleExceptionHandler
) : ILookupRepository
{
    private const string GenderQuery = @"
        SELECT id, title
          FROM genders
      ORDER BY id";

    private const string EmploymentTypeQuery = @"
        SELECT id, title
          FROM employment_types
      ORDER BY id";

    private const string EmploymentStatusQuery = @"
        SELECT id, title
          FROM employment_status
      ORDER BY id";

    public async Task<IEnumerable<LookupItem>> GetGendersAsync()
        => await ReadAsync(GenderQuery);

    public async Task<IEnumerable<LookupItem>> GetEmploymentTypesAsync()
        => await ReadAsync(EmploymentTypeQuery);

    public async Task<IEnumerable<LookupItem>> GetEmploymentStatusesAsync()
        => await ReadAsync(EmploymentStatusQuery);

    private async Task<IEnumerable<LookupItem>> ReadAsync(string query)
    {
        var items = new List<LookupItem>();

        try
        {
            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            using (var command = new OracleCommand(query, (OracleConnection)connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    items.Add(new LookupItem
                    (
                        Id: reader.GetDouble(reader.GetOrdinal("id")),
                        Title: reader.GetString(reader.GetOrdinal("title"))
                    ));
                }
            }

            return items;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw;
        }
    }
}
