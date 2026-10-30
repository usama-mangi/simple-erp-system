using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
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
                employees";

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

            throw;
        }
    }

    public async Task<EmployeeWithDetails?> GetByIdAsync(double id)
    {
        string query = @"
            SELECT
                e.id as employee_id, e.first_name, e.last_name, e.email, e.phone, e.date_of_birth, e.gender_id, e.address, e.created_at, e.updated_at as emp_updated_at,
                ed.job_title, ed.salary, ed.start_date, ed.end_date, ed.employment_type_id, ed.employment_status_id, ed.updated_at as dep_updated_at,
                d.id as department_id, d.name as department_name
            FROM
                employees e
            JOIN
                employee_details ed ON ed.employee_id = e.id
            JOIN
                departments d ON e.department_id = d.id 
            WHERE
                e.id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Double) { Value = id });

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

            throw;
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
            throw;
        }
    }

    public async Task CreateWithDetailsAsync(EmployeeWithDetails employee)
    {
        string empQuery = @"
            INSERT INTO
                employees (first_name, last_name, email, phone, date_of_birth, gender_id, address)
            VALUES
                (:FirstName, :LastName, :Email, :Phone, :DateOfBirth, :GenderId, :Address)";
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
                var empCommand = new OracleCommand(empQuery, (OracleConnection)connection)
                {
                    BindByName = true
                };
                empCommand.Parameters.Add(":FirstName", OracleDbType.Varchar2, employee.FirstName, ParameterDirection.Input);
                empCommand.Parameters.Add(":LastName", OracleDbType.Varchar2, employee.LastName, ParameterDirection.Input);
                empCommand.Parameters.Add(":Email", OracleDbType.Varchar2, employee.Email, ParameterDirection.Input);
                empCommand.Parameters.Add(":Phone", OracleDbType.Varchar2, employee.Phone, ParameterDirection.Input);
                empCommand.Parameters.Add(":DateOfBirth", OracleDbType.Date, employee.DateOfBirth, ParameterDirection.Input);
                empCommand.Parameters.Add(":GenderId", OracleDbType.Double, employee.GenderId, ParameterDirection.Input);
                empCommand.Parameters.Add(":Address", OracleDbType.Varchar2, employee.Address, ParameterDirection.Input);

                var detCommand = new OracleCommand(detQuery, (OracleConnection)connection)
                {
                    BindByName = true
                };
                detCommand.Parameters.Add(":EmployeeId", OracleDbType.Double, employee.Id, ParameterDirection.Input);
                detCommand.Parameters.Add(":JobTitle", OracleDbType.Varchar2, employee.JobTitle, ParameterDirection.Input);
                detCommand.Parameters.Add(":Salary", OracleDbType.Double, employee.Salary, ParameterDirection.Input);
                detCommand.Parameters.Add(":StartDate", OracleDbType.Date, employee.StartDate, ParameterDirection.Input);
                detCommand.Parameters.Add(":EndDate", OracleDbType.Date, employee.EndDate, ParameterDirection.Input);
                detCommand.Parameters.Add(":DepartmentId", OracleDbType.Double, employee.DepartmentId, ParameterDirection.Input);
                detCommand.Parameters.Add(":EmploymentTypeId", OracleDbType.Double, employee.EmploymentTypeId, ParameterDirection.Input);
                detCommand.Parameters.Add(":EmploymentStatusId", OracleDbType.Double, employee.EmploymentStatusId, ParameterDirection.Input);

                await empCommand.ExecuteNonQueryAsync();
                await detCommand.ExecuteNonQueryAsync();

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
                    new DuplicateEmailException(ex),
                    new DuplicatePhoneException(ex),
                    new EndDateOlderThanStartDateException(ex),
                    new InvalidSalaryException(ex)
                ])
                .HandleCreateExceptions(ex);
            throw;
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
                employees
            SET
                job_title = :JobTitle,
                salary = :Salary,
                start_date = :StartDate,
                department_id = :DepartmentId,
                employment_type_id = :EmployementTypeId,
                employment_status_id = :EmploymentStatusId";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var empCommand = new OracleCommand(empQuery, (OracleConnection)connection)
                {
                    BindByName = true
                };
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
                detCommand.Parameters.Add(":JobTitle", OracleDbType.Varchar2, employee.JobTitle, ParameterDirection.Input);
                detCommand.Parameters.Add(":Salary", OracleDbType.Double, employee.Salary, ParameterDirection.Input);
                detCommand.Parameters.Add(":StartDate", OracleDbType.Date, employee.StartDate, ParameterDirection.Input);
                detCommand.Parameters.Add(":DepartmentId", OracleDbType.Double, employee.DepartmentId, ParameterDirection.Input);
                detCommand.Parameters.Add(":EmploymentTypeId", OracleDbType.Double, employee.EmploymentTypeId, ParameterDirection.Input);
                detCommand.Parameters.Add(":EmploymentStatusId", OracleDbType.Double, employee.EmploymentStatusId, ParameterDirection.Input);

                await empCommand.ExecuteNonQueryAsync();
                await detCommand.ExecuteNonQueryAsync();

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
                    new DuplicateEmailException(ex),
                    new DuplicatePhoneException(ex),
                    new InvalidSalaryException(ex),
                    new EndDateOlderThanStartDateException(ex)
                ])
                .HandleUpdateExceptions(ex);
            throw;
        }
    }

    public async Task DeleteAsync(double id)
    {
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
                .HandleUpdateExceptions(ex)
                .HandleDeleteExceptions(ex);
            throw;
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
        var empUpdatedAtOrdinal = reader.GetOrdinal("emp_updated_at");
        var detUpdatedAtOrdinal = reader.GetOrdinal("det_updated_at");

        DateTime updatedAt;

        updatedAt = reader.IsDBNull(empUpdatedAtOrdinal) ? default : reader.GetDateTime(empUpdatedAtOrdinal);
        if (!reader.IsDBNull(detUpdatedAtOrdinal) && updatedAt != default)
        {
            var detUpdatedAt = reader.GetDateTime(detUpdatedAtOrdinal);
            updatedAt = detUpdatedAt > updatedAt ? detUpdatedAt : updatedAt;
        }

        return new EmployeeWithDetails
        {
            Id = reader.GetDouble(reader.GetOrdinal("id")),
            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
            LastName = reader.GetString(reader.GetOrdinal("last_name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            Phone = reader.GetString(reader.GetOrdinal("phone")),
            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("date_of_birth")),
            GenderId = reader.GetDouble(reader.GetOrdinal("gender_id")),
            Address = reader.GetString(reader.GetOrdinal("address")),
            JobTitle = reader.GetString(reader.GetOrdinal("job_title")),
            Salary = reader.GetDouble(reader.GetOrdinal("salary")),
            StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
            EndDate = reader.GetDateTime(reader.GetOrdinal("end_date")),
            DepartmentId = reader.GetDouble(reader.GetOrdinal("department_id")),
            DepartmentName = reader.GetString(reader.GetOrdinal("department_name")),
            EmploymentTypeId = reader.GetDouble(reader.GetOrdinal("employment_type_id")),
            EmploymentStatusId = reader.GetDouble(reader.GetOrdinal("employment_status_id")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = updatedAt
        };
    }
}