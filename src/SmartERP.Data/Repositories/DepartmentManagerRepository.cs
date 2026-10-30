using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Constraint.DepartmentManager;
using SmartERP.Data.Exceptions.Constraint.EmployeeDetail;
using SmartERP.Data.Exceptions.Read;
using SmartERP.Data.Exceptions.Update;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// Note: the schema's uq_dept_current_manager binds one manager to one
// department at a time; the schema does NOT forbid two managers for one
// department, so that guard is intentionally left to the unique index only.
public class DepartmentManagerRepository(
    IDbConnectionFactory _dbConnection,
    OracleExceptionHandler _oracleExceptionHandler
) : IDepartmentManagerRepository
{
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

    public async Task<DepartmentManager?> GetCurrentByDepartmentAsync(double departmentId)
    {
        // uq_dept_current_manager guarantees at most one OPEN row per manager;
        // a department may legally show none, so a miss returns null.
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                dm.department_id = :Id
              AND dm.end_date IS NULL";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Int64) { Value = (long)departmentId });

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

    public async Task<IEnumerable<DepartmentManager>> GetHistoryAsync(double departmentId)
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

    public async Task AssignAsync(DepartmentManager assignment)
    {
        if (assignment.EndDate is { })
            await AssignWithNetTransactionAsync(assignment); // history backfill has an end date
        else
            await AssignWithStoredProcedureAsync(assignment);
    }

    // db/05_procedures.sql: ends the department's current open assignment and
    // inserts the new one atomically. The procedure does not commit; the caller
    // owns the transaction, per the spec's lesson on COMMIT placement inside
    // procedures (a failing procedure exits by exception and the caller's
    // rollback still covers everything).
    private async Task AssignWithStoredProcedureAsync(DepartmentManager assignment)
    {
        string call = "sp_assign_manager";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                using var command = new OracleCommand(call, (OracleConnection)connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Transaction = (OracleTransaction)transaction;

                // Positional IN parameters (BindByName has no meaning for procs).
                command.Parameters.Add(new OracleParameter("p_department_id", OracleDbType.Double) { Value = assignment.DepartmentId });
                command.Parameters.Add(new OracleParameter("p_employee_id", OracleDbType.Double) { Value = assignment.ManagerId });
                command.Parameters.Add(new OracleParameter("p_start_date", OracleDbType.Date) { Value = assignment.StartDate });

                await command.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [new DuplicateCurrentManagerException(ex)])
                .HandleCreateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    private async Task AssignWithNetTransactionAsync(DepartmentManager assignment)
    {
        string closeQuery = @"
            UPDATE
                department_managers
            SET
                end_date = :EndDate
            WHERE
                department_id = :DepartmentId
              AND end_date IS NULL";

        string insertQuery = @"
            INSERT INTO
                department_managers (department_id, manager_id, start_date, end_date)
            VALUES
                (:DepartmentId, :ManagerId, :StartDate, :EndDate)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                using (var closeCommand = new OracleCommand(closeQuery, (OracleConnection)connection))
                {
                    closeCommand.Transaction = (OracleTransaction)transaction;
                    closeCommand.Parameters.Add(":EndDate", OracleDbType.Date, assignment.EndDate!.Value, ParameterDirection.Input);
                    closeCommand.Parameters.Add(":DepartmentId", OracleDbType.Double, assignment.DepartmentId, ParameterDirection.Input);
                    await closeCommand.ExecuteNonQueryAsync();
                }

                using (var insertCommand = new OracleCommand(insertQuery, (OracleConnection)connection))
                {
                    insertCommand.Transaction = (OracleTransaction)transaction;
                    insertCommand.Parameters.Add(":DepartmentId", OracleDbType.Double, assignment.DepartmentId, ParameterDirection.Input);
                    insertCommand.Parameters.Add(":ManagerId", OracleDbType.Double, assignment.ManagerId, ParameterDirection.Input);
                    insertCommand.Parameters.Add(":StartDate", OracleDbType.Date, assignment.StartDate, ParameterDirection.Input);
                    insertCommand.Parameters.Add(":EndDate", OracleDbType.Date, assignment.EndDate!.Value, ParameterDirection.Input);
                    await insertCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [
                    new DuplicateCurrentManagerException(ex),
                    new EndDateOlderThanStartDateException(ex)
                ])
                .HandleCreateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task EndCurrentAsync(double departmentId, DateOnly endDate)
    {
        // Department-scoped close: only the open row is touched; closed history
        // stays immutable and repeated closes fail loudly instead of silently
        // rewriting a historical row.
        string query = @"
            UPDATE
                department_managers
            SET
                end_date = :EndDate
            WHERE
                department_id = :DepartmentId
              AND end_date IS NULL";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            command.Parameters.Add(":EndDate", OracleDbType.Date, endDate.ToDateTime(TimeOnly.MinValue), ParameterDirection.Input);
            command.Parameters.Add(":DepartmentId", OracleDbType.Double, departmentId, ParameterDirection.Input);

            var affected = await command.ExecuteNonQueryAsync();

            if (affected == 0)
            {
                throw new NotFoundException(nameof(DepartmentManager), departmentId);
            }
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [new EndDateOlderThanStartDateException(ex)])
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
