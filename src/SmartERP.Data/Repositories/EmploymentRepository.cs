using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Constraint.EmployeeDetail;
using SmartERP.Data.Exceptions.Read;
using SmartERP.Data.Exceptions.Update;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class EmploymentRepository(
    IDbConnectionFactory _dbConnection,
    OracleExceptionHandler _oracleExceptionHandler
) : IEmploymentRepository
{
    private const string SelectColumns = @"
                ed.id, ed.employee_id, ed.job_title, ed.salary, ed.start_date, ed.end_date,
                ed.department_id, d.name as department_name,
                ed.employment_type_id, ed.employment_status_id,
                ed.created_at, ed.updated_at";

    private const string FromClause = @"
            FROM
                employment_details ed
            LEFT JOIN
                departments d ON ed.department_id = d.id";

    public async Task<IEnumerable<EmploymentDetail>> GetByEmployeeAsync(double employeeId)
    {
        // Order: open row first, then history from most recent to oldest.
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                ed.employee_id = :Id
            ORDER BY
                CASE WHEN ed.end_date IS NULL THEN 0 ELSE 1 END,
                ed.start_date DESC";

        try
        {
            var details = new List<EmploymentDetail>();
            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            using (var command = new OracleCommand(query, (OracleConnection)connection))
            {
                command.BindByName = true;
                command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Int64) { Value = (long)employeeId });

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    details.Add(MapToDetail(reader));
                }
            }

            return details;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<EmploymentDetail?> GetCurrentAsync(double employeeId)
    {
        // uq_det_current_job guarantees at most one open row per employee.
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                ed.employee_id = :Id
              AND ed.end_date IS NULL";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Int64) { Value = (long)employeeId });

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToDetail(reader);
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

    public async Task<EmploymentDetail?> GetByIdAsync(double id)
    {
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                ed.id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Int64) { Value = (long)id });

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToDetail(reader);
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

    public async Task CreateAsync(EmploymentDetail detail)
    {
        string query = @"
            INSERT INTO
                employment_details (employee_id, job_title, salary, start_date, end_date, department_id, employment_type_id, employment_status_id)
            VALUES
                (:EmployeeId, :JobTitle, :Salary, :StartDate, :EndDate, :DepartmentId, :EmploymentTypeId, :EmploymentStatusId)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            AddInsertParameters(command, detail);
            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [
                    new DuplicateCurrentJobException(ex),
                    new EndDateOlderThanStartDateException(ex),
                    new InvalidSalaryException(ex)
                ])
                .HandleCreateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task CreateReplacingCurrentAsync(EmploymentDetail detail)
    {
        // Replaces an open assignment under one transaction: the open row ends
        // (its end_date becomes the new assignment's start date, satisfying the
        // chk_det_end_date check of the OLD row since old start < new start),
        // then the new row inserts into the slot uq_det_current_job frees up.
        // If the insert fails, the close rolls back too. The old row's own
        // start-date gate is validated by the caller before this runs.
        string closeQuery = @"
            UPDATE
                employment_details
            SET
                end_date = :CloseOn
            WHERE
                employee_id = :EmployeeId
              AND end_date IS NULL";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();

            // Spec transaction pattern: await using, async begin/commit/rollback,
            // commands explicitly bound to the transaction.
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                using (var closeCommand = new OracleCommand(closeQuery, (OracleConnection)connection))
                {
                    closeCommand.Transaction = (OracleTransaction)transaction;
                    closeCommand.Parameters.Add(":CloseOn", OracleDbType.Date, detail.StartDate, ParameterDirection.Input);
                    closeCommand.Parameters.Add(":EmployeeId", OracleDbType.Double, detail.EmployeeId, ParameterDirection.Input);
                    await closeCommand.ExecuteNonQueryAsync();
                }

                using (var insertCommand = new OracleCommand(BuildInsertQuery(), (OracleConnection)connection))
                {
                    insertCommand.Transaction = (OracleTransaction)transaction;
                    AddInsertParameters(insertCommand, detail);
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
                    new EndDateOlderThanStartDateException(ex),
                    new InvalidSalaryException(ex)
                ])
                .HandleCreateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task EndCurrentAsync(double employeeId, DateOnly endDate)
    {
        // Employee-scoped close: end_date IS NULL keeps closed history immutable
        // and makes repeated closes fail loudly instead of silently rewriting
        // a historical row.
        string query = @"
            UPDATE
                employment_details
            SET
                end_date = :EndDate
            WHERE
                employee_id = :EmployeeId
              AND end_date IS NULL";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            command.Parameters.Add(":EndDate", OracleDbType.Date, endDate.ToDateTime(TimeOnly.MinValue), ParameterDirection.Input);
            command.Parameters.Add(":EmployeeId", OracleDbType.Double, employeeId, ParameterDirection.Input);

            var affected = await command.ExecuteNonQueryAsync();

            if (affected == 0)
            {
                throw new NotFoundException(nameof(EmploymentDetail), employeeId);
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

    public async Task TransferCurrentDepartmentAsync(double employeeId, double departmentId)
    {
        // Direct repoint of the open assignment: a department transfer does not
        // create history; only closing the row does. FK_DET_DEPARTMENT enforces
        // that the target department exists (ORA-02291 -> typed parent missing).
        string query = @"
            UPDATE
                employment_details
            SET
                department_id = :DepartmentId
            WHERE
                employee_id = :EmployeeId
              AND end_date IS NULL";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            command.Parameters.Add(":DepartmentId", OracleDbType.Double, departmentId, ParameterDirection.Input);
            command.Parameters.Add(":EmployeeId", OracleDbType.Double, employeeId, ParameterDirection.Input);

            var affected = await command.ExecuteNonQueryAsync();

            if (affected == 0)
            {
                throw new NotFoundException(nameof(EmploymentDetail), employeeId);
            }
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleUpdateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    private static string BuildInsertQuery()
        => @"
            INSERT INTO
                employment_details (employee_id, job_title, salary, start_date, end_date, department_id, employment_type_id, employment_status_id)
            VALUES
                (:EmployeeId, :JobTitle, :Salary, :StartDate, :EndDate, :DepartmentId, :EmploymentTypeId, :EmploymentStatusId)";

    private static void AddInsertParameters(OracleCommand command, EmploymentDetail detail)
    {
        command.Parameters.Add(":EmployeeId", OracleDbType.Double, detail.EmployeeId, ParameterDirection.Input);
        command.Parameters.Add(":JobTitle", OracleDbType.Varchar2, detail.JobTitle, ParameterDirection.Input);
        command.Parameters.Add(":Salary", OracleDbType.Double, detail.Salary, ParameterDirection.Input);
        command.Parameters.Add(":StartDate", OracleDbType.Date, detail.StartDate, ParameterDirection.Input);
        command.Parameters.Add(
            ":EndDate",
            OracleDbType.Date,
            detail.EndDate is { } endDate ? endDate : DBNull.Value,
            ParameterDirection.Input
        );
        command.Parameters.Add(":DepartmentId", OracleDbType.Double, detail.DepartmentId, ParameterDirection.Input);
        command.Parameters.Add(":EmploymentTypeId", OracleDbType.Double, detail.EmploymentTypeId, ParameterDirection.Input);
        command.Parameters.Add(":EmploymentStatusId", OracleDbType.Double, detail.EmploymentStatusId, ParameterDirection.Input);
    }

    private static EmploymentDetail MapToDetail(OracleDataReader reader)
    {
        int endDateOrdinal = reader.GetOrdinal("end_date");
        int updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new EmploymentDetail
        {
            Id = reader.GetDouble(reader.GetOrdinal("id")),
            EmployeeId = reader.GetDouble(reader.GetOrdinal("employee_id")),
            JobTitle = reader.GetString(reader.GetOrdinal("job_title")),
            Salary = reader.GetDouble(reader.GetOrdinal("salary")),
            StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
            EndDate = reader.IsDBNull(endDateOrdinal) ? null : reader.GetDateTime(endDateOrdinal),
            DepartmentId = reader.GetDouble(reader.GetOrdinal("department_id")),
            DepartmentName = reader.IsDBNull(reader.GetOrdinal("department_name"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("department_name")),
            EmploymentTypeId = reader.GetDouble(reader.GetOrdinal("employment_type_id")),
            EmploymentStatusId = reader.GetDouble(reader.GetOrdinal("employment_status_id")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? default : reader.GetDateTime(updatedAtOrdinal)
        };
    }
}
