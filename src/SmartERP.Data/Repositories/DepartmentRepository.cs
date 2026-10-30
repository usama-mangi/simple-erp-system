using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.Exceptions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions.Constraint.Department;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class DepartmentRepository(IDbConnectionFactory _dbConnection, OracleExceptionHandler _oracleExceptionHandler) : IDepartmentRepository
{
    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        var departments = new List<Department>();

        string query = @"
            SELECT
                id, name, code, location, established, created_at, updated_at
            FROM
                departments";

        try
        {
            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            using (var command = new OracleCommand(query, (OracleConnection)connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    departments.Add(MapToDepartment(reader));
                }
            }

            return departments;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<Department?> GetByIdAsync(double id)
    {
        string query = @"
            SELECT
                id, name, code, location, established, created_at, updated_at
            FROM
                departments
            WHERE
                id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Double) { Value = id });

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDepartment(reader);
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

    public async Task CreateAsync(Department department)
    {
        string query = @"
            INSERT INTO departments
                (name, code, location, established)
            VALUES
                (:Name, :Code, :Location, :Established)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(":Name", OracleDbType.Varchar2, department.Name, ParameterDirection.Input);
            command.Parameters.Add(":Code", OracleDbType.Varchar2, department.Code, ParameterDirection.Input);
            command.Parameters.Add(":Location", OracleDbType.Varchar2, department.Location, ParameterDirection.Input);
            command.Parameters.Add(":Established", OracleDbType.Date, department.Established, ParameterDirection.Input);

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex)
                .HandleConstraintExceptions(ex, [
                    new DuplicateDepartmentCodeException(ex),
                    new DuplicateDepartmentNameException(ex)
                ]);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task UpdateAsync(Department department)
    {
        string query = @"
            UPDATE
                departments
            SET
                name        = :Name,
                code        = :Code,
                location    = :Location,
                established = :Established
            WHERE
                id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(":Id", OracleDbType.Double, department.Id, ParameterDirection.Input);
            command.Parameters.Add(":Name", OracleDbType.Varchar2, department.Name, ParameterDirection.Input);
            command.Parameters.Add(":Code", OracleDbType.Varchar2, department.Code, ParameterDirection.Input);
            command.Parameters.Add(":Location", OracleDbType.Varchar2, department.Location, ParameterDirection.Input);
            command.Parameters.Add(":Established", OracleDbType.Date, department.Established, ParameterDirection.Input);

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [
                    new DuplicateDepartmentCodeException(ex),
                    new DuplicateDepartmentNameException(ex)
                ])
                .HandleUpdateExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<string?> DeleteAsync(double id)
    {
        string query = @"
            DELETE FROM
                departments
            WHERE
                id = :Id
            RETURNING
                name INTO :DeptName";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(":Id", OracleDbType.Double, id, ParameterDirection.Input);
            var DeptName = command.Parameters.Add(":DeptName", OracleDbType.Varchar2, ParameterDirection.Output);

            await command.ExecuteNonQueryAsync();

            if (DeptName == null) return null;
            else return DeptName.Value.ToString();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleDeleteExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    private static Department MapToDepartment(OracleDataReader reader)
    {
        int updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new Department
        {
            Id = reader.GetDouble(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Code = reader.GetString(reader.GetOrdinal("code")),
            Established = reader.GetDateTime(reader.GetOrdinal("established")),
            Location = reader.GetString(reader.GetOrdinal("location")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),

            UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? default : reader.GetDateTime(updatedAtOrdinal)
        };
    }
}