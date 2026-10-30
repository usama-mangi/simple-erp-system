using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmartERP.Data.Abstractions;
using SmartERP.Data.Exceptions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions.Constraint.Employee;
using SmartERP.Data.Exceptions.Constraint.EmployeeDetail;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class EmployeeRepository(
    IDbConnectionFactory _dbConnection,
    OracleExceptionHandler _oracleExceptionHandler
) : IEmployeeRepository
{
    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        var employees = new List<Employee>();

        string query = @"
            SELECT
                id, first_name, last_name, email, phone, date_of_birth, gender_id, address, created_at, updated_at
            FROM
                employees
            WHERE
                is_deleted = 0";

        try
        {
            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            using (var command = new OracleCommand(query, (OracleConnection)connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    employees.Add(MapToEmployee(reader));
                }
            }

            return employees;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<EmployeeWithDetails?> GetByIdAsync(double id)
    {
        // LEFT JOIN: an employee may exist without any employment details row yet.
        // The end_date IS NULL filter pins the join to the current assignment;
        // uq_det_current_job guarantees at most one open row per employee.
        string query = @"
            SELECT
                e.id as employee_id, e.first_name, e.last_name, e.email, e.phone, e.date_of_birth, e.gender_id, e.address, e.created_at, e.updated_at as emp_updated_at,
                ed.job_title, ed.salary, ed.start_date, ed.end_date, ed.employment_type_id, ed.employment_status_id, ed.updated_at as det_updated_at,
                d.id as department_id, d.name as department_name
            FROM
                employees e
            LEFT JOIN
                employment_details ed ON ed.employee_id = e.id AND ed.end_date IS NULL
            LEFT JOIN
                departments d ON ed.department_id = d.id
            WHERE
                e.id = :Id
              AND e.is_deleted = 0";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Int64) { Value = (long)id });

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToEmployeeWithDetails(reader);
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

    public async Task CreateAsync(Employee employee)
    {
        string query = @"
            INSERT INTO
                employees (first_name, last_name, email, phone, date_of_birth, gender_id, address)
            VALUES
                (:FirstName, :LastName, :Email, :Phone, :DateOfBirth, :GenderId, :Address)";

        try
        {
            var connection = await _dbConnection.CreateOpenConnectionAsync();
            var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };
            command.Parameters.Add(":FirstName", OracleDbType.Varchar2, employee.FirstName, ParameterDirection.Input);
            command.Parameters.Add(":LastName", OracleDbType.Varchar2, employee.LastName, ParameterDirection.Input);
            command.Parameters.Add(":Email", OracleDbType.Varchar2, employee.Email, ParameterDirection.Input);
            command.Parameters.Add(":Phone", OracleDbType.Varchar2, employee.Phone, ParameterDirection.Input);
            command.Parameters.Add(":DateOfBirth", OracleDbType.Date, employee.DateOfBirth, ParameterDirection.Input);
            command.Parameters.Add(":GenderId", OracleDbType.Double, employee.GenderId, ParameterDirection.Input);
            command.Parameters.Add(":Address", OracleDbType.Varchar2, employee.Address, ParameterDirection.Input);

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [
                    new DuplicateEmailException(ex),
                    new DuplicatePhoneException(ex)
                ])
                .HandleCreateExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task CreateWithDetailsAsync(EmployeeWithDetails employee)
    {
        string empQuery = @"
            INSERT INTO
                employees (first_name, last_name, email, phone, date_of_birth, gender_id, address)
            VALUES
                (:FirstName, :LastName, :Email, :Phone, :DateOfBirth, :GenderId, :Address)
            RETURNING id INTO :NewId";
        string detQuery = @"
            INSERT INTO
                employment_details (employee_id, job_title, salary, start_date, end_date, department_id, employment_type_id, employment_status_id)
            VALUES
                (:EmployeeId, :JobTitle, :Salary, :StartDate, :EndDate, :DepartmentId, :EmploymentTypeId, :EmploymentStatusId)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            // Spec transaction pattern: await using, async begin/commit/rollback,
            // commands explicitly bound to the transaction.
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var empCommand = new OracleCommand(empQuery, (OracleConnection)connection)
                {
                    BindByName = true,
                    Transaction = (OracleTransaction)transaction
                };
                empCommand.Parameters.Add(":FirstName", OracleDbType.Varchar2, employee.FirstName, ParameterDirection.Input);
                empCommand.Parameters.Add(":LastName", OracleDbType.Varchar2, employee.LastName, ParameterDirection.Input);
                empCommand.Parameters.Add(":Email", OracleDbType.Varchar2, employee.Email, ParameterDirection.Input);
                empCommand.Parameters.Add(":Phone", OracleDbType.Varchar2, employee.Phone, ParameterDirection.Input);
                empCommand.Parameters.Add(":DateOfBirth", OracleDbType.Date, employee.DateOfBirth, ParameterDirection.Input);
                empCommand.Parameters.Add(":GenderId", OracleDbType.Double, employee.GenderId, ParameterDirection.Input);
                empCommand.Parameters.Add(":Address", OracleDbType.Varchar2, employee.Address, ParameterDirection.Input);
                var newIdParam = empCommand.Parameters.Add(
                    new OracleParameter(":NewId", OracleDbType.Int64) { Direction = ParameterDirection.Output }
                );

                var detCommand = new OracleCommand(detQuery, (OracleConnection)connection)
                {
                    BindByName = true,
                    Transaction = (OracleTransaction)transaction
                };
                detCommand.Parameters.Add(":JobTitle", OracleDbType.Varchar2, employee.JobTitle, ParameterDirection.Input);
                detCommand.Parameters.Add(":Salary", OracleDbType.Double, employee.Salary, ParameterDirection.Input);
                detCommand.Parameters.Add(":StartDate", OracleDbType.Date, employee.StartDate, ParameterDirection.Input);
                detCommand.Parameters.Add(":EndDate", OracleDbType.Date, (object?)employee.EndDate ?? DBNull.Value, ParameterDirection.Input);
                detCommand.Parameters.Add(":DepartmentId", OracleDbType.Double, employee.DepartmentId, ParameterDirection.Input);
                detCommand.Parameters.Add(":EmploymentTypeId", OracleDbType.Double, employee.EmploymentTypeId, ParameterDirection.Input);
                detCommand.Parameters.Add(":EmploymentStatusId", OracleDbType.Double, employee.EmploymentStatusId, ParameterDirection.Input);

                await empCommand.ExecuteNonQueryAsync();

                // Identity PK is issued by Oracle and only known after the insert;
                // feed it into the detail row instead of trusting the caller's Id.
                // ODP.NET returns the output binding as OracleDecimal, which does
                // not implement IConvertible - cast through the type directly.
                var returnedId = (OracleDecimal)newIdParam.Value;

                if (returnedId.IsNull)
                    throw new InvalidOperationException("Employee insert did not return the generated identity id.");

                employee.Id = (double)returnedId;
                detCommand.Parameters.Add(":EmployeeId", OracleDbType.Double, employee.Id, ParameterDirection.Input);

                await detCommand.ExecuteNonQueryAsync();

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
                    new DuplicateEmailException(ex),
                    new DuplicatePhoneException(ex),
                    new EndDateOlderThanStartDateException(ex),
                    new InvalidSalaryException(ex)
                ])
                .HandleCreateExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task UpdateAsync(EmployeeWithDetails employee)
    {
        string empQuery = @"
            UPDATE
                employees
            SET
                first_name = :FirstName,
                last_name = :LastName,
                email = :Email,
                phone = :Phone,
                date_of_birth = :DateOfBirth,
                gender_id = :GenderId,
                address = :Address
            WHERE
                id = :Id";
        string detQuery = @"
            UPDATE
                employment_details
            SET
                job_title = :JobTitle,
                salary = :Salary,
                start_date = :StartDate,
                department_id = :DepartmentId,
                employment_type_id = :EmploymentTypeId,
                employment_status_id = :EmploymentStatusId
            WHERE
                employee_id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();

            // Spec transaction pattern: await using, async begin/commit/rollback,
            // commands explicitly bound to the transaction.
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                using var empCommand = new OracleCommand(empQuery, (OracleConnection)connection)
                {
                    BindByName = true,
                    Transaction = (OracleTransaction)transaction
                };
                empCommand.Parameters.Add(":Id", OracleDbType.Double, employee.Id, ParameterDirection.Input);
                empCommand.Parameters.Add(":FirstName", OracleDbType.Varchar2, employee.FirstName, ParameterDirection.Input);
                empCommand.Parameters.Add(":LastName", OracleDbType.Varchar2, employee.LastName, ParameterDirection.Input);
                empCommand.Parameters.Add(":Email", OracleDbType.Varchar2, employee.Email, ParameterDirection.Input);
                empCommand.Parameters.Add(":Phone", OracleDbType.Varchar2, employee.Phone, ParameterDirection.Input);
                empCommand.Parameters.Add(":DateOfBirth", OracleDbType.Date, employee.DateOfBirth, ParameterDirection.Input);
                empCommand.Parameters.Add(":GenderId", OracleDbType.Double, employee.GenderId, ParameterDirection.Input);
                empCommand.Parameters.Add(":Address", OracleDbType.Varchar2, employee.Address, ParameterDirection.Input);

                using var detCommand = new OracleCommand(detQuery, (OracleConnection)connection)
                {
                    BindByName = true
                };
                detCommand.Parameters.Add(":Id", OracleDbType.Double, employee.Id, ParameterDirection.Input);
                detCommand.Parameters.Add(":JobTitle", OracleDbType.Varchar2, employee.JobTitle, ParameterDirection.Input);
                detCommand.Parameters.Add(":Salary", OracleDbType.Double, employee.Salary, ParameterDirection.Input);
                detCommand.Parameters.Add(":StartDate", OracleDbType.Date, employee.StartDate, ParameterDirection.Input);
                detCommand.Parameters.Add(":DepartmentId", OracleDbType.Double, employee.DepartmentId, ParameterDirection.Input);
                detCommand.Parameters.Add(":EmploymentTypeId", OracleDbType.Double, employee.EmploymentTypeId, ParameterDirection.Input);
                detCommand.Parameters.Add(":EmploymentStatusId", OracleDbType.Double, employee.EmploymentStatusId, ParameterDirection.Input);

                // Updates only the open employment_details row (end_date IS NULL is the
                // history boundary; uq_det_current_job guarantees a single candidate row).
                // An employee without an open row matches zero rows and stays unchanged.
                await empCommand.ExecuteNonQueryAsync();
                await detCommand.ExecuteNonQueryAsync();

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
                    new DuplicateEmailException(ex),
                    new DuplicatePhoneException(ex),
                    new InvalidSalaryException(ex),
                    new EndDateOlderThanStartDateException(ex)
                ])
                .HandleUpdateExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<bool> HasDependenciesAsync(double id)
    {
        // One round-trip: both child-table counts in a single scalar.
        // employment_details holds every job row (open and closed history),
        // department_managers every management assignment.
        string query = @"
            SELECT
                ( SELECT COUNT(*)
                    FROM employment_details
                   WHERE employee_id = :Id ) +
                ( SELECT COUNT(*)
                    FROM department_managers
                   WHERE manager_id = :Id ) AS dep_count
            FROM dual";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Int64) { Value = (long)id });

            // ExecuteScalar shape differs per statement type: COUNT over dual
            // surfaces as System.Decimal, RETURNING output surfaced as
            // OracleDecimal. Normalize both instead of hard-casting one shape.
            var result = await command.ExecuteScalarAsync();

            var count = result switch
            {
                null => 0m, // no row returned: nothing can depend on this id
                decimal d => d,
                OracleDecimal od => od.IsNull ? 0m : od.Value,
                _ => Convert.ToDecimal(result)
            };

            return count > 0;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task DeleteAsync(double id)
    {
        // Soft delete: is_deleted=1 keeps every child FK intact and consistent.
        // GetAllAsync / GetByIdAsync hide rows through the is_deleted = 0 filter,
        // so nothing downstream ever sees the flagged row.
        string query = @"
            UPDATE
                employees
            SET
                is_deleted = 1
            WHERE
                id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.Parameters.Add(":Id", OracleDbType.Double, id, ParameterDirection.Input);

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

    public async Task DeleteWithDetailsAsync(double id)
    {
        // Destructive purge: the employee row plus every dependent record.
        // Children go first in one transaction; if any statement fails the
        // rollback restores all three, so the employee either survives fully
        // or vanishes fully. ChildFoundException cannot fire on this path
        // because no children remain when the employees delete runs.
        string detQuery = @"
            DELETE FROM
                employment_details
            WHERE
                employee_id = :Id";

        string mgrQuery = @"
            DELETE FROM
                department_managers
            WHERE
                manager_id = :Id";

        string empQuery = @"
            DELETE FROM
                employees
            WHERE
                id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            // Spec transaction pattern: await using, async begin/commit/rollback,
            // commands explicitly bound to the transaction.
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                using (var detCommand = new OracleCommand(detQuery, (OracleConnection)connection))
                {
                    detCommand.Transaction = (OracleTransaction)transaction;
                    detCommand.Parameters.Add(":Id", OracleDbType.Double, id, ParameterDirection.Input);
                    await detCommand.ExecuteNonQueryAsync();
                }

                using (var mgrCommand = new OracleCommand(mgrQuery, (OracleConnection)connection))
                {
                    mgrCommand.Transaction = (OracleTransaction)transaction;
                    mgrCommand.Parameters.Add(":Id", OracleDbType.Double, id, ParameterDirection.Input);
                    await mgrCommand.ExecuteNonQueryAsync();
                }

                using (var empCommand = new OracleCommand(empQuery, (OracleConnection)connection))
                {
                    empCommand.Transaction = (OracleTransaction)transaction;
                    empCommand.Parameters.Add(":Id", OracleDbType.Double, id, ParameterDirection.Input);
                    await empCommand.ExecuteNonQueryAsync();
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
                .HandleDeleteExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    private static Employee MapToEmployee(OracleDataReader reader)
    {
        int updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new Employee
        {
            Id = reader.GetDouble(reader.GetOrdinal("id")),
            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
            LastName = reader.GetString(reader.GetOrdinal("last_name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            Phone = reader.GetString(reader.GetOrdinal("phone")),
            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("date_of_birth")),
            GenderId = reader.GetDouble(reader.GetOrdinal("gender_id")),
            Address = reader.GetString(reader.GetOrdinal("address")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? default : reader.GetDateTime(updatedAtOrdinal)
        };
    }

    private static EmployeeWithDetails MapToEmployeeWithDetails(OracleDataReader reader)
    {
        var detUpdatedAtOrdinal = reader.GetOrdinal("det_updated_at");

        bool hasDetails = !reader.IsDBNull(reader.GetOrdinal("job_title"));

        DateTime updatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"));

        if (hasDetails && !reader.IsDBNull(detUpdatedAtOrdinal))
        {
            var empUpdatedAtOrdinal = reader.GetOrdinal("emp_updated_at");
            var detUpdatedAt = reader.GetDateTime(detUpdatedAtOrdinal);

            if (!reader.IsDBNull(empUpdatedAtOrdinal) && detUpdatedAt < reader.GetDateTime(empUpdatedAtOrdinal))
            {
                updatedAt = reader.GetDateTime(empUpdatedAtOrdinal);
            }
            else
            {
                updatedAt = detUpdatedAt;
            }
        }

        int endDateOrdinal = reader.GetOrdinal("end_date");

        return new EmployeeWithDetails
        {
            Id = reader.GetDouble(reader.GetOrdinal("employee_id")),
            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
            LastName = reader.GetString(reader.GetOrdinal("last_name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            Phone = reader.GetString(reader.GetOrdinal("phone")),
            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("date_of_birth")),
            GenderId = reader.GetDouble(reader.GetOrdinal("gender_id")),
            Address = reader.GetString(reader.GetOrdinal("address")),
            HasEmploymentDetails = hasDetails,
            JobTitle = hasDetails ? reader.GetString(reader.GetOrdinal("job_title")) : string.Empty,
            Salary = hasDetails ? reader.GetDouble(reader.GetOrdinal("salary")) : 0,
            StartDate = hasDetails ? reader.GetDateTime(reader.GetOrdinal("start_date")) : default,
            EndDate = !hasDetails || reader.IsDBNull(endDateOrdinal) ? null : reader.GetDateTime(endDateOrdinal),
            DepartmentId = hasDetails ? reader.GetDouble(reader.GetOrdinal("department_id")) : 0,
            DepartmentName = hasDetails ? reader.GetString(reader.GetOrdinal("department_name")) : string.Empty,
            EmploymentTypeId = hasDetails ? reader.GetDouble(reader.GetOrdinal("employment_type_id")) : 0,
            EmploymentStatusId = hasDetails ? reader.GetDouble(reader.GetOrdinal("employment_status_id")) : 0,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = updatedAt
        };
    }
}