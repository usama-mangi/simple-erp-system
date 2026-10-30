using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.Exceptions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Constraint.DepartmentManager;
using SmartERP.Data.Exceptions.Read;
using SmartERP.Data.Exceptions.Update;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class DepartmentManagersRepository(
    IDbConnectionFactory _dbConnection,
    OracleExceptionHandler _oracleExceptionHandler
) : IDepartmentManagersRepository
{
    // Note: the schema's uq_dept_current_manager binds one manager to one
    // department at a time; the schema does NOT forbid two managers for one
    // department, so that guard is intentionally left to the unique index only.

    private const string SelectColumns = @"
                dm.id, dm.department_id, d.name as department_name,
                dm.manager_id,
                ( SELECT e.first_name || ' ' || e.last_name
                    FROM employees e
                   WHERE e.id = dm.manager_id ) as manager_name,
                dm.start_date, dm.end_date, dm.created_at, dm.updated_at";

    private const string FromClause = @"
            FROM
                department_managers dm
            LEFT JOIN
                departments d ON dm.department_id = d.id";

    public async Task<IEnumerable<DepartmentManager>> GetAllByDepartmentAsync(double departmentId)
    {
        // Order: open row first, then history from most recent to oldest.
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                dm.department_id = :Id
            ORDER BY
                CASE WHEN dm.end_date IS NULL THEN 0 ELSE 1 END,
                dm.start_date DESC";

        try
        {
            var assignments = new List<DepartmentManager>();

            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            using (var command = new OracleCommand(query, (OracleConnection)connection))
            {
                command.BindByName = true;
                command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Int64) { Value = (long)departmentId });

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    assignments.Add(MapToAssignment(reader));
                }
            }

            return assignments;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<DepartmentManager?> GetByIdAsync(double id)
    {
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                dm.id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Int64) { Value = (long)id });

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToAssignment(reader);
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

    public async Task CreateAsync(DepartmentManager assignment)
    {
        string query = @"
            INSERT INTO
                department_managers (department_id, manager_id, start_date, end_date)
            VALUES
                (:DepartmentId, :ManagerId, :StartDate, :EndDate)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            command.Parameters.Add(":DepartmentId", OracleDbType.Double, assignment.DepartmentId, ParameterDirection.Input);
            command.Parameters.Add(":ManagerId", OracleDbType.Double, assignment.ManagerId, ParameterDirection.Input);
            command.Parameters.Add(":StartDate", OracleDbType.Date, assignment.StartDate, ParameterDirection.Input);
            command.Parameters.Add(
                ":EndDate",
                OracleDbType.Date,
                assignment.EndDate is { } endDate ? endDate : DBNull.Value,
                ParameterDirection.Input
            );

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [
                    new DuplicateCurrentManagerException(ex)
                ])
                .HandleCreateExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task CloseAsync(double id, DateOnly endDate)
    {
        // Only an open row closes: end_date IS NULL keeps closed history
        // immutable and makes repeated closes fail loudly instead of silently
        // rewriting a historical row.
        string query = @"
            UPDATE
                department_managers
            SET
                end_date = :EndDate
            WHERE
                id = :Id
              AND end_date IS NULL";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            command.Parameters.Add(":EndDate", OracleDbType.Date, endDate.ToDateTime(TimeOnly.MinValue), ParameterDirection.Input);
            command.Parameters.Add(":Id", OracleDbType.Double, id, ParameterDirection.Input);

            var affected = await command.ExecuteNonQueryAsync();

            if (affected == 0)
            {
                throw new NotFoundException(nameof(DepartmentManager), id);
            }
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [new DuplicateCurrentManagerException(ex)])
                .HandleUpdateExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    private static DepartmentManager MapToAssignment(OracleDataReader reader)
    {
        int endDateOrdinal = reader.GetOrdinal("end_date");
        int updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new DepartmentManager
        {
            Id = reader.GetDouble(reader.GetOrdinal("id")),
            DepartmentId = reader.GetDouble(reader.GetOrdinal("department_id")),
            DepartmentName = reader.IsDBNull(reader.GetOrdinal("department_name"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("department_name")),
            ManagerId = reader.GetDouble(reader.GetOrdinal("manager_id")),
            ManagerName = reader.IsDBNull(reader.GetOrdinal("manager_name"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("manager_name")),
            StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
            EndDate = reader.IsDBNull(endDateOrdinal) ? null : reader.GetDateTime(endDateOrdinal),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? default : reader.GetDateTime(updatedAtOrdinal)
        };
    }
}
