using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class LookupRepository(IDbConnectionFactory _dbConnection, IOracleExceptionHandler _oracleExceptionHandler) : ILookupRepository
{
    public async Task<IEnumerable<LookupItem>> GetGendersAsync()
    {
        return await GetItemsAsync("genders", "title");
    }

    public async Task<IEnumerable<LookupItem>> GetEmploymentTypesAsync()
    {
        return await GetItemsAsync("employment_types", "title");
    }

    public async Task<IEnumerable<LookupItem>> GetEmploymentStatusesAsync()
    {
        return await GetItemsAsync("employment_status", "title");
    }

    public async Task<IEnumerable<LookupItem>> GetPositionsAsync()
    {
        return await GetItemsAsync("positions", "title");
    }

    public async Task<IEnumerable<LookupItem>> GetLeaveTypesAsync()
    {
        return await GetItemsAsync("leave_types", "title");
    }

    public async Task<IEnumerable<LookupItem>> GetLeaveStatusesAsync()
    {
        return await GetItemsAsync("leave_statuses", "title");
    }

    public async Task<IEnumerable<LookupItem>> GetDocumentTypesAsync()
    {
        return await GetItemsAsync("document_types", "title");
    }

    public async Task<IEnumerable<LookupItem>> GetApplicationStagesAsync()
    {
        return await GetItemsAsync("application_stages", "title");
    }

    private async Task<IEnumerable<LookupItem>> GetItemsAsync(string tableName, string titleColumn)
    {
        string query = $"SELECT id, {titleColumn} FROM {tableName}";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            using var reader = await command.ExecuteReaderAsync();

            var items = new List<LookupItem>();
            while (await reader.ReadAsync())
            {
                items.Add(new LookupItem
                (
                    reader.GetDecimal(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal(titleColumn))
                ));
            }

            return items;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }
}
