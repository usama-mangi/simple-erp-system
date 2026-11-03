using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class ApplicationStageRepository(
    IDbConnectionFactory _dbConnection,
    IOracleExceptionHandler _oracleExceptionHandler
) : IApplicationStageRepository
{
    private const string SelectStageColumns = @"
        id, title, description, is_active, sort_order,
        created_at, updated_at";
    private const string FromClause = @"FROM application_stages";

    public async Task<IEnumerable<LookupItem>> GetApplicationStagesAsync()
    {
        string query = $"SELECT id, title FROM application_stages ORDER BY sort_order";

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
                    reader.GetString(reader.GetOrdinal("title"))
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

    public async Task<IEnumerable<ApplicationStage>> GetAllStagesAsync()
    {
        string query = $"SELECT {SelectStageColumns} {FromClause} ORDER BY sort_order";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            using var reader = await command.ExecuteReaderAsync();

            var stages = new List<ApplicationStage>();
            while (await reader.ReadAsync())
            {
                stages.Add(new ApplicationStage
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    IsActive = reader.GetInt32(reader.GetOrdinal("is_active")) == 1,
                    SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return stages;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<ApplicationStage?> GetStageByIdAsync(decimal id)
    {
        string query = $"SELECT {SelectStageColumns} {FromClause} WHERE id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p = new OracleParameter(":Id", OracleDbType.Decimal);
            p.Value = id;
            command.Parameters.Add(p);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ApplicationStage
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    IsActive = reader.GetInt32(reader.GetOrdinal("is_active")) == 1,
                    SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };
            }

            return null;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<ApplicationStage?> GetStageByTitleAsync(string title)
    {
        string query = $"SELECT {SelectStageColumns} {FromClause} WHERE title = :Title";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p = new OracleParameter(":Title", OracleDbType.Varchar2);
            p.Value = title;
            command.Parameters.Add(p);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ApplicationStage
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    IsActive = reader.GetInt32(reader.GetOrdinal("is_active")) == 1,
                    SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };
            }

            return null;
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
