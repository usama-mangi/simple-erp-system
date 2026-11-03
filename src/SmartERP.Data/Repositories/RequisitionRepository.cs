using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class RequisitionRepository(
    IDbConnectionFactory _dbConnection,
    IOracleExceptionHandler _oracleExceptionHandler
) : IRequisitionRepository
{
    private const string SelectRequisitionColumns = @"
        r.id, r.title, r.description, r.salary, r.openings,
        r.status_id, s.title as status_title,
        r.created_at, r.updated_at,
        d.name as department_name, p.title as position_title";
    private const string FromClause = @"
        FROM requisitions r
        JOIN departments d ON d.id = r.department_id
        JOIN positions p ON p.id = r.position_id
        JOIN application_stages s ON s.id = r.status_id";

    public async Task<IEnumerable<Requisition>> GetAllRequisitionsAsync()
    {
        string query = $"SELECT {SelectRequisitionColumns} {FromClause}";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            using var reader = await command.ExecuteReaderAsync();

            var requisitions = new List<Requisition>();
            while (await reader.ReadAsync())
            {
                requisitions.Add(new Requisition
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    DepartmentId = reader.GetDecimal(reader.GetOrdinal("department_id")),
                    DepartmentName = reader.GetString(reader.GetOrdinal("department_name")),
                    PositionId = reader.GetDecimal(reader.GetOrdinal("position_id")),
                    PositionTitle = reader.GetString(reader.GetOrdinal("position_title")),
                    Salary = reader.GetDecimal(reader.GetOrdinal("salary")),
                    Openings = reader.GetInt32(reader.GetOrdinal("openings")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return requisitions;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<IEnumerable<Requisition>> GetActiveRequisitionsAsync()
    {
        string query = $"SELECT {SelectRequisitionColumns} {FromClause} WHERE r.status_id = 1";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            using var reader = await command.ExecuteReaderAsync();

            var requisitions = new List<Requisition>();
            while (await reader.ReadAsync())
            {
                requisitions.Add(new Requisition
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    DepartmentId = reader.GetDecimal(reader.GetOrdinal("department_id")),
                    DepartmentName = reader.GetString(reader.GetOrdinal("department_name")),
                    PositionId = reader.GetDecimal(reader.GetOrdinal("position_id")),
                    PositionTitle = reader.GetString(reader.GetOrdinal("position_title")),
                    Salary = reader.GetDecimal(reader.GetOrdinal("salary")),
                    Openings = reader.GetInt32(reader.GetOrdinal("openings")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return requisitions;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<Requisition?> GetRequisitionByIdAsync(decimal id)
    {
        string query = $"SELECT {SelectRequisitionColumns} {FromClause} WHERE r.id = :Id";

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
                return new Requisition
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    DepartmentId = reader.GetDecimal(reader.GetOrdinal("department_id")),
                    DepartmentName = reader.GetString(reader.GetOrdinal("department_name")),
                    PositionId = reader.GetDecimal(reader.GetOrdinal("position_id")),
                    PositionTitle = reader.GetString(reader.GetOrdinal("position_title")),
                    Salary = reader.GetDecimal(reader.GetOrdinal("salary")),
                    Openings = reader.GetInt32(reader.GetOrdinal("openings")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
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

    public async Task<Requisition?> GetRequisitionByTitleAsync(string title)
    {
        string query = $"SELECT {SelectRequisitionColumns} {FromClause} WHERE r.title = :Title";

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
                return new Requisition
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    DepartmentId = reader.GetDecimal(reader.GetOrdinal("department_id")),
                    DepartmentName = reader.GetString(reader.GetOrdinal("department_name")),
                    PositionId = reader.GetDecimal(reader.GetOrdinal("position_id")),
                    PositionTitle = reader.GetString(reader.GetOrdinal("position_title")),
                    Salary = reader.GetDecimal(reader.GetOrdinal("salary")),
                    Openings = reader.GetInt32(reader.GetOrdinal("openings")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
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

    public async Task CreateRequisitionAsync(Requisition requisition)
    {
        string query = @"
            INSERT INTO requisitions
                (title, description, department_id, position_id, salary, openings, status_id, created_at, updated_at)
            VALUES
                (:Title, :Description, :DepartmentId, :PositionId, :Salary, :Openings, :StatusId, :CreatedAt, :UpdatedAt)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p1 = new OracleParameter(":Title", OracleDbType.Varchar2); p1.Value = requisition.Title; command.Parameters.Add(p1);
            var p2 = new OracleParameter(":Description", OracleDbType.Varchar2); p2.Value = requisition.Description != null ? (object)requisition.Description : DBNull.Value; command.Parameters.Add(p2);
            var p3 = new OracleParameter(":DepartmentId", OracleDbType.Decimal); p3.Value = requisition.DepartmentId; command.Parameters.Add(p3);
            var p4 = new OracleParameter(":PositionId", OracleDbType.Decimal); p4.Value = requisition.PositionId; command.Parameters.Add(p4);
            var p5 = new OracleParameter(":Salary", OracleDbType.Decimal); p5.Value = requisition.Salary; command.Parameters.Add(p5);
            var p6 = new OracleParameter(":Openings", OracleDbType.Int32); p6.Value = requisition.Openings; command.Parameters.Add(p6);
            var p7 = new OracleParameter(":StatusId", OracleDbType.Decimal); p7.Value = requisition.StatusId; command.Parameters.Add(p7);
            var p8 = new OracleParameter(":CreatedAt", OracleDbType.Date); p8.Value = requisition.CreatedAt; command.Parameters.Add(p8);
            var p9 = new OracleParameter(":UpdatedAt", OracleDbType.Date); p9.Value = requisition.UpdatedAt.HasValue ? requisition.UpdatedAt.Value : (object)DBNull.Value; command.Parameters.Add(p9);

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleCreateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task UpdateRequisitionAsync(Requisition requisition)
    {
        string query = @"
            UPDATE requisitions
            SET title = :Title,
                description = :Description,
                department_id = :DepartmentId,
                position_id = :PositionId,
                salary = :Salary,
                openings = :Openings,
                status_id = :StatusId,
                updated_at = :UpdatedAt
            WHERE id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p1 = new OracleParameter(":Id", OracleDbType.Decimal); p1.Value = requisition.Id; command.Parameters.Add(p1);
            var p2 = new OracleParameter(":Title", OracleDbType.Varchar2); p2.Value = requisition.Title; command.Parameters.Add(p2);
            var p3 = new OracleParameter(":Description", OracleDbType.Varchar2); p3.Value = requisition.Description != null ? (object)requisition.Description : DBNull.Value; command.Parameters.Add(p3);
            var p4 = new OracleParameter(":DepartmentId", OracleDbType.Decimal); p4.Value = requisition.DepartmentId; command.Parameters.Add(p4);
            var p5 = new OracleParameter(":PositionId", OracleDbType.Decimal); p5.Value = requisition.PositionId; command.Parameters.Add(p5);
            var p6 = new OracleParameter(":Salary", OracleDbType.Decimal); p6.Value = requisition.Salary; command.Parameters.Add(p6);
            var p7 = new OracleParameter(":Openings", OracleDbType.Int32); p7.Value = requisition.Openings; command.Parameters.Add(p7);
            var p8 = new OracleParameter(":StatusId", OracleDbType.Decimal); p8.Value = requisition.StatusId; command.Parameters.Add(p8);
            var p9 = new OracleParameter(":UpdatedAt", OracleDbType.Date); p9.Value = requisition.UpdatedAt.HasValue ? requisition.UpdatedAt.Value : (object)DBNull.Value; command.Parameters.Add(p9);

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleUpdateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task DeleteRequisitionAsync(decimal id)
    {
        string query = "DELETE FROM requisitions WHERE id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p = new OracleParameter(":Id", OracleDbType.Decimal);
            p.Value = id;
            command.Parameters.Add(p);

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleDeleteExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }
}
