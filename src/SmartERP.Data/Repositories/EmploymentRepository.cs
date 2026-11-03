using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
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
    IOracleExceptionHandler _oracleExceptionHandler
) : IEmploymentRepository
{
    private const string SelectColumns = @"
                ed.id, ed.employee_id, ed.position_id, p.title as position_name, ed.salary, ed.start_date, ed.end_date,
                ed.department_id, d.name as department_name,
                ed.employment_type_id, ed.employment_status_id,
                ed.created_at, ed.updated_at";

    private const string FromClause = @"
            FROM
                employment_details ed
            LEFT JOIN
                positions p ON p.id = ed.position_id
            LEFT JOIN
                departments d ON ed.department_id = d.id";

    public async Task<IEnumerable<EmploymentDetail>> GetByEmployeeAsync(decimal employeeId)
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

    public async Task<EmploymentDetail?> GetCurrentAsync(decimal employeeId)
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

    public async Task<EmploymentDetail?> GetByIdAsync(decimal id)
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
                employment_details (employee_id, position_id, salary, start_date, end_date, department_id, employment_type_id, employment_status_id)
            VALUES
                (:EmployeeId, :PositionId, :Salary, :StartDate, :EndDate, :DepartmentId, :EmploymentTypeId, :EmploymentStatusId)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                using (var command = new OracleCommand(query, (OracleConnection)connection)
                {
                    BindByName = true,
                    Transaction = (OracleTransaction)transaction
                })
                {
                    AddInsertParameters(command, detail);
                    await command.ExecuteNonQueryAsync();
                }

                await StatusEventWriter.InsertAsync(
                    (OracleConnection)connection,
                    (OracleTransaction)transaction,
                    detail.EmployeeId,
                    null,
                    detail.EmploymentStatusId,
                    detail.StartDate,
                    "New assignment started");

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
              AND end_date IS NULL
            RETURNING
                employment_status_id INTO :FromStatus";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();

            // Spec transaction pattern: await using, async begin/commit/rollback,
            // commands explicitly bound to the transaction.
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                decimal? fromStatus = null;
                using (var closeCommand = new OracleCommand(closeQuery, (OracleConnection)connection))
                {
                    closeCommand.Transaction = (OracleTransaction)transaction;
                    closeCommand.Parameters.Add(":CloseOn", OracleDbType.Date, detail.StartDate, ParameterDirection.Input);
                    closeCommand.Parameters.Add(":EmployeeId", OracleDbType.Decimal, detail.EmployeeId, ParameterDirection.Input);
                    var fromParam = closeCommand.Parameters.Add(
                        new OracleParameter(":FromStatus", OracleDbType.Decimal) { Direction = ParameterDirection.Output });
                    await closeCommand.ExecuteNonQueryAsync();
                    if (fromParam.Value is OracleDecimal od && !od.IsNull)
                    {
                        fromStatus = od.Value;
                    }
                }

                using (var insertCommand = new OracleCommand(BuildInsertQuery(), (OracleConnection)connection))
                {
                    insertCommand.Transaction = (OracleTransaction)transaction;
                    AddInsertParameters(insertCommand, detail);
                    await insertCommand.ExecuteNonQueryAsync();
                }

                await StatusEventWriter.InsertAsync(
                    (OracleConnection)connection,
                    (OracleTransaction)transaction,
                    detail.EmployeeId,
                    fromStatus,
                    detail.EmploymentStatusId,
                    detail.StartDate,
                    "Replaced the previous assignment");

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

    public async Task EndCurrentAsync(decimal employeeId, DateOnly endDate)
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
            command.Parameters.Add(":EmployeeId", OracleDbType.Decimal, employeeId, ParameterDirection.Input);

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

    public async Task TransferCurrentDepartmentAsync(decimal employeeId, decimal departmentId)
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

            command.Parameters.Add(":DepartmentId", OracleDbType.Decimal, departmentId, ParameterDirection.Input);
            command.Parameters.Add(":EmployeeId", OracleDbType.Decimal, employeeId, ParameterDirection.Input);

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
                employment_details (employee_id, position_id, salary, start_date, end_date, department_id, employment_type_id, employment_status_id)
            VALUES
                (:EmployeeId, :PositionId, :Salary, :StartDate, :EndDate, :DepartmentId, :EmploymentTypeId, :EmploymentStatusId)";

    private static void AddInsertParameters(OracleCommand command, EmploymentDetail detail)
    {
        command.Parameters.Add(":EmployeeId", OracleDbType.Decimal, detail.EmployeeId, ParameterDirection.Input);
        command.Parameters.Add(":PositionId", OracleDbType.Decimal, detail.PositionId, ParameterDirection.Input);
        command.Parameters.Add(":Salary", OracleDbType.Double, detail.Salary, ParameterDirection.Input);
        command.Parameters.Add(":StartDate", OracleDbType.Date, detail.StartDate, ParameterDirection.Input);
        command.Parameters.Add(
            ":EndDate",
            OracleDbType.Date,
            detail.EndDate is { } endDate ? endDate : DBNull.Value,
            ParameterDirection.Input
        );
        command.Parameters.Add(":DepartmentId", OracleDbType.Decimal, detail.DepartmentId, ParameterDirection.Input);
        command.Parameters.Add(":EmploymentTypeId", OracleDbType.Decimal, detail.EmploymentTypeId, ParameterDirection.Input);
        command.Parameters.Add(":EmploymentStatusId", OracleDbType.Decimal, detail.EmploymentStatusId, ParameterDirection.Input);
    }

    public async Task<IEnumerable<StatusEvent>> GetStatusEventsAsync(decimal employeeId)
    {
        string query = @"
            SELECT
                se.id, se.employee_id, se.from_status_id, fs.title AS from_status_title,
                se.to_status_id, ts.title AS to_status_title, se.changed_at, se.note
            FROM
                status_events se
            LEFT JOIN
                employment_status fs ON fs.id = se.from_status_id
            JOIN
                employment_status ts ON ts.id = se.to_status_id
            WHERE
                se.employee_id = :Id
            ORDER BY
                se.changed_at DESC, se.id DESC";

        try
        {
            var events = new List<StatusEvent>();
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);
            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Int64) { Value = (long)employeeId });

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                events.Add(MapToStatusEvent(reader));
            }
            return events;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    private static StatusEvent MapToStatusEvent(OracleDataReader reader)
    {
        int fromStatusOrdinal = reader.GetOrdinal("from_status_id");
        int fromTitleOrdinal = reader.GetOrdinal("from_status_title");
        int noteOrdinal = reader.GetOrdinal("note");
        return new StatusEvent
        {
            Id = reader.GetDecimal(reader.GetOrdinal("id")),
            EmployeeId = reader.GetDecimal(reader.GetOrdinal("employee_id")),
            FromStatusId = reader.IsDBNull(fromStatusOrdinal) ? null : reader.GetDecimal(fromStatusOrdinal),
            FromStatusTitle = reader.IsDBNull(fromTitleOrdinal) ? null : reader.GetString(fromTitleOrdinal),
            ToStatusId = reader.GetDecimal(reader.GetOrdinal("to_status_id")),
            ToStatusTitle = reader.GetString(reader.GetOrdinal("to_status_title")),
            ChangedAt = reader.GetDateTime(reader.GetOrdinal("changed_at")),
            Note = reader.IsDBNull(noteOrdinal) ? null : reader.GetString(noteOrdinal)
        };
    }

    private static EmploymentDetail MapToDetail(OracleDataReader reader)
    {
        int endDateOrdinal = reader.GetOrdinal("end_date");
        int updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new EmploymentDetail
        {
            Id = reader.GetDecimal(reader.GetOrdinal("id")),
            EmployeeId = reader.GetDecimal(reader.GetOrdinal("employee_id")),
            PositionId = reader.GetDecimal(reader.GetOrdinal("position_id")),
            PositionName = reader.GetString(reader.GetOrdinal("position_name")),
            Salary = reader.GetDouble(reader.GetOrdinal("salary")),
            StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
            EndDate = reader.IsDBNull(endDateOrdinal) ? null : reader.GetDateTime(endDateOrdinal),
            DepartmentId = reader.GetDecimal(reader.GetOrdinal("department_id")),
            DepartmentName = reader.IsDBNull(reader.GetOrdinal("department_name"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("department_name")),
            EmploymentTypeId = reader.GetDecimal(reader.GetOrdinal("employment_type_id")),
            EmploymentStatusId = reader.GetDecimal(reader.GetOrdinal("employment_status_id")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? default : reader.GetDateTime(updatedAtOrdinal)
        };
    }
}
