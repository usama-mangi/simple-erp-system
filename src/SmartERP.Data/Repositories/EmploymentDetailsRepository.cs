using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.Exceptions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions.Create;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Constraint.EmployeeDetail;
using SmartERP.Data.Exceptions.Read;
using SmartERP.Data.Exceptions.Update;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class EmploymentDetailsRepository(
    IDbConnectionFactory _dbConnection,
    OracleExceptionHandler _oracleExceptionHandler
) : IEmploymentDetailsRepository
{
    public async Task<IEnumerable<EmploymentDetail>> GetAllByEmployeeAsync(double employeeId)
    {
        // Order: open row first, then history from most recent to oldest.
        string query = @"
            SELECT
                ed.id, ed.employee_id, ed.job_title, ed.salary, ed.start_date, ed.end_date,
                ed.department_id, d.name as department_name,
                ed.employment_type_id, ed.employment_status_id,
                ed.created_at, ed.updated_at
            FROM
                employment_details ed
            LEFT JOIN
                departments d ON ed.department_id = d.id
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

    public async Task<EmploymentDetail?> GetByIdAsync(double id)
    {
        string query = @"
            SELECT
                ed.id, ed.employee_id, ed.job_title, ed.salary, ed.start_date, ed.end_date,
                ed.department_id, d.name as department_name,
                ed.employment_type_id, ed.employment_status_id,
                ed.created_at, ed.updated_at
            FROM
                employment_details ed
            LEFT JOIN
                departments d ON ed.department_id = d.id
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
        // If the insert fails, the close rolls back too.
        string closeQuery = @"
            UPDATE
                employment_details
            SET
                end_date = :CloseOn
            WHERE
                employee_id = :EmployeeId
              AND end_date IS NULL";

        string detQuery = @"
            INSERT INTO
                employment_details (employee_id, job_title, salary, start_date, end_date, department_id, employment_type_id, employment_status_id)
            VALUES
                (:EmployeeId, :JobTitle, :Salary, :StartDate, :EndDate, :DepartmentId, :EmploymentTypeId, :EmploymentStatusId)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var closeCommand = new OracleCommand(closeQuery, (OracleConnection)connection)
                {
                    BindByName = true
                };
                closeCommand.Parameters.Add(":CloseOn", OracleDbType.Date, detail.StartDate, ParameterDirection.Input);
                closeCommand.Parameters.Add(":EmployeeId", OracleDbType.Double, detail.EmployeeId, ParameterDirection.Input);
                await closeCommand.ExecuteNonQueryAsync();

                using var insertCommand = new OracleCommand(detQuery, (OracleConnection)connection)
                {
                    BindByName = true
                };
                insertCommand.Parameters.Add(":EmployeeId", OracleDbType.Double, detail.EmployeeId, ParameterDirection.Input);
                insertCommand.Parameters.Add(":JobTitle", OracleDbType.Varchar2, detail.JobTitle, ParameterDirection.Input);
                insertCommand.Parameters.Add(":Salary", OracleDbType.Double, detail.Salary, ParameterDirection.Input);
                insertCommand.Parameters.Add(":StartDate", OracleDbType.Date, detail.StartDate, ParameterDirection.Input);
                insertCommand.Parameters.Add(
                    ":EndDate",
                    OracleDbType.Date,
                    detail.EndDate is { } endDate ? endDate : DBNull.Value,
                    ParameterDirection.Input
                );
                insertCommand.Parameters.Add(":DepartmentId", OracleDbType.Double, detail.DepartmentId, ParameterDirection.Input);
                insertCommand.Parameters.Add(":EmploymentTypeId", OracleDbType.Double, detail.EmploymentTypeId, ParameterDirection.Input);
                insertCommand.Parameters.Add(":EmploymentStatusId", OracleDbType.Double, detail.EmploymentStatusId, ParameterDirection.Input);
                await insertCommand.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
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

    public async Task CloseAsync(double id, DateOnly endDate)
    {
        // Only an open row closes: end_date IS NULL in the WHERE keeps closed
        // history immutable and makes the call idempotent-safe (zero matches).
        string query = @"
            UPDATE
                employment_details
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
                // The row was already closed or does not exist; either way the
                // caller asked to close something that cannot close again.
                throw new NotFoundException(nameof(EmploymentDetail), id);
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
