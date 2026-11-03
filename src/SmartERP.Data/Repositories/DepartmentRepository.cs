using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.Exceptions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions.Constraint.Department;
using SmartERP.Data.Models;
using Oracle.ManagedDataAccess.Types;

namespace SmartERP.Data.Repositories;

public class DepartmentRepository(IDbConnectionFactory _dbConnection, IOracleExceptionHandler _oracleExceptionHandler) : IDepartmentRepository
{
    private const string DepartmentColumns = @"
                d.id, d.name, d.code, d.location, d.established,
                d.parent_department_id, p.name AS parent_name,
                d.created_at, d.updated_at";

    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        var departments = new List<Department>();

        string query = $@"
            SELECT
                {DepartmentColumns}
            FROM
                departments d
            LEFT JOIN
                departments p ON p.id = d.parent_department_id
            ORDER BY
                d.name";

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

    public async Task<Department?> GetByIdAsync(decimal id)
    {
        string query = $@"
            SELECT
                {DepartmentColumns}
            FROM
                departments d
            LEFT JOIN
                departments p ON p.id = d.parent_department_id
            WHERE
                d.id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Decimal) { Value = id });

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
                (name, code, location, established, parent_department_id)
            VALUES
                (:Name, :Code, :Location, :Established, :ParentDepartmentId)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(":Name", OracleDbType.Varchar2, department.Name, ParameterDirection.Input);
            command.Parameters.Add(":Code", OracleDbType.Varchar2, department.Code, ParameterDirection.Input);
            command.Parameters.Add(":Location", OracleDbType.Varchar2, department.Location, ParameterDirection.Input);
            command.Parameters.Add(":Established", OracleDbType.Date, department.Established, ParameterDirection.Input);
            command.Parameters.Add(":ParentDepartmentId", OracleDbType.Decimal,
                (object?)department.ParentDepartmentId ?? DBNull.Value, ParameterDirection.Input);

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex)
                .HandleConstraintExceptions(ex, [
                    new DuplicateDepartmentCodeException(ex),
                    new DuplicateDepartmentNameException(ex),
                    new DepartmentParentSelfException(ex)
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
                name                 = :Name,
                code                 = :Code,
                location             = :Location,
                established          = :Established,
                parent_department_id = :ParentDepartmentId
            WHERE
                id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(":Id", OracleDbType.Decimal, department.Id, ParameterDirection.Input);
            command.Parameters.Add(":Name", OracleDbType.Varchar2, department.Name, ParameterDirection.Input);
            command.Parameters.Add(":Code", OracleDbType.Varchar2, department.Code, ParameterDirection.Input);
            command.Parameters.Add(":Location", OracleDbType.Varchar2, department.Location, ParameterDirection.Input);
            command.Parameters.Add(":Established", OracleDbType.Date, department.Established, ParameterDirection.Input);
            command.Parameters.Add(":ParentDepartmentId", OracleDbType.Decimal,
                (object?)department.ParentDepartmentId ?? DBNull.Value, ParameterDirection.Input);

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [
                    new DuplicateDepartmentCodeException(ex),
                    new DuplicateDepartmentNameException(ex),
                    new DepartmentParentSelfException(ex)
                ])
                .HandleUpdateExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<bool> HasDependenciesAsync(decimal id)
    {
        // One round-trip, mirroring the employee check: all three blocker
        // tables in a single scalar over dual.
        string query = @"
            SELECT
                ( SELECT COUNT(*)
                    FROM employment_details
                   WHERE department_id = :Id ) +
                ( SELECT COUNT(*)
                    FROM department_managers
                   WHERE department_id = :Id ) +
                ( SELECT COUNT(*)
                    FROM departments
                   WHERE parent_department_id = :Id ) AS dep_count
            FROM dual";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Decimal) { Value = id });

            var result = await command.ExecuteScalarAsync();

            var count = result switch
            {
                null => 0m,
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

    public async Task<string?> DeleteAsync(decimal id)
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
            command.Parameters.Add(":Id", OracleDbType.Decimal, id, ParameterDirection.Input);
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

    public async Task<IEnumerable<DepartmentNode>> GetHierarchyAsync()
    {
        // dept_tree walks roots down to their reports in depth-first order via
        // sort_path. subtree expands every department into itself plus all of
        // its descendants, and rollup counts the distinct employees holding an
        // open job anywhere in that subtree. Direct headcount uses the same
        // open-job + non-deleted-employee definition, so the two columns agree.
        string query = @"
            WITH dept_tree (
                id, name, code, parent_department_id, parent_name, depth, sort_path
            ) AS (
                SELECT
                    d.id,
                    d.name,
                    d.code,
                    d.parent_department_id,
                    CAST(NULL AS VARCHAR2(30)),
                    0,
                    CAST(d.name AS VARCHAR2(4000))
                FROM
                    departments d
                WHERE
                    d.parent_department_id IS NULL
                UNION ALL
                SELECT
                    c.id,
                    c.name,
                    c.code,
                    c.parent_department_id,
                    p.name,
                    t.depth + 1,
                    CAST(t.sort_path || ' / ' || c.name AS VARCHAR2(4000))
                FROM
                    departments c
                JOIN
                    dept_tree t ON c.parent_department_id = t.id
                JOIN
                    departments p ON p.id = c.parent_department_id
            ),
            subtree (root_id, descendant_id) AS (
                SELECT
                    d.id,
                    d.id
                FROM
                    departments d
                UNION ALL
                SELECT
                    s.root_id,
                    c.id
                FROM
                    subtree s
                JOIN
                    departments c ON c.parent_department_id = s.descendant_id
            ),
            rollup AS (
                SELECT
                    s.root_id,
                    COUNT(DISTINCT ed.employee_id) AS rollup_headcount
                FROM
                    subtree s
                JOIN
                    employment_details ed
                    ON  ed.department_id = s.descendant_id
                    AND ed.end_date IS NULL
                JOIN
                    employees e
                    ON  e.id = ed.employee_id
                    AND e.is_deleted = 0
                GROUP BY
                    s.root_id
            )
            SELECT
                t.id,
                t.name,
                t.code,
                t.parent_department_id,
                t.parent_name,
                t.depth,
                (
                    SELECT
                        COUNT(*)
                    FROM
                        employment_details ed
                    JOIN
                        employees e
                        ON  e.id = ed.employee_id
                        AND e.is_deleted = 0
                    WHERE
                        ed.department_id = t.id
                        AND ed.end_date IS NULL
                ) AS direct_headcount,
                NVL(r.rollup_headcount, 0) AS rollup_headcount
            FROM
                dept_tree t
            LEFT JOIN
                rollup r ON r.root_id = t.id
            ORDER BY
                t.sort_path";

        var nodes = new List<DepartmentNode>();

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                nodes.Add(MapToDepartmentNode(reader));
            }

            return nodes;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<IEnumerable<Department>> GetParentCandidatesAsync(decimal? excludeDepartmentId)
    {
        // A null exclude id produces a one-row CTE holding NULL, which matches
        // no department, so every row survives the NOT EXISTS. With an id, the
        // CTE walks that department's whole subtree so it cannot be chosen as
        // its own ancestor.
        string query = $@"
            WITH descendants ( id ) AS (
                SELECT
                    :ExcludeDepartmentId
                FROM
                    dual
                UNION ALL
                SELECT
                    d.id
                FROM
                    departments d
                JOIN
                    descendants x ON d.parent_department_id = x.id
            )
            SELECT
                {DepartmentColumns}
            FROM
                departments d
            LEFT JOIN
                departments p ON p.id = d.parent_department_id
            WHERE
                NOT EXISTS (
                    SELECT 1
                    FROM descendants x
                    WHERE x.id = d.id
                )
            ORDER BY
                d.name";

        var departments = new List<Department>();

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(":ExcludeDepartmentId", OracleDbType.Decimal,
                (object?)excludeDepartmentId ?? DBNull.Value, ParameterDirection.Input);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                departments.Add(MapToDepartment(reader));
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

    public async Task<bool> WouldCreateCycleAsync(decimal departmentId, decimal parentDepartmentId)
    {
        // Walk up from the proposed parent. If the department being edited
        // appears in that ancestor chain, the new parent is itself a
        // descendant, i.e. the pair would create a loop.
        string query = @"
            SELECT
                COUNT(*)
            FROM (
                SELECT
                    id
                FROM
                    departments
                START WITH
                    id = :ParentDepartmentId
                CONNECT BY NOCYCLE PRIOR parent_department_id = id
            )
            WHERE
                id = :DepartmentId";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(":ParentDepartmentId", OracleDbType.Decimal, parentDepartmentId, ParameterDirection.Input);
            command.Parameters.Add(":DepartmentId", OracleDbType.Decimal, departmentId, ParameterDirection.Input);

            var count = await command.ExecuteScalarAsync();

            return count != null && Convert.ToInt32(count) > 0;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    private static Department MapToDepartment(OracleDataReader reader)
    {
        int updatedAtOrdinal = reader.GetOrdinal("updated_at");
        int parentIdOrdinal = reader.GetOrdinal("parent_department_id");
        int parentNameOrdinal = reader.GetOrdinal("parent_name");

        return new Department
        {
            Id = reader.GetDecimal(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Code = reader.GetString(reader.GetOrdinal("code")),
            Established = reader.GetDateTime(reader.GetOrdinal("established")),
            Location = reader.GetString(reader.GetOrdinal("location")),
            ParentDepartmentId = reader.IsDBNull(parentIdOrdinal) ? null : reader.GetDecimal(parentIdOrdinal),
            ParentName = reader.IsDBNull(parentNameOrdinal) ? null : reader.GetString(parentNameOrdinal),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),

            UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? default : reader.GetDateTime(updatedAtOrdinal)
        };
    }

    private static DepartmentNode MapToDepartmentNode(OracleDataReader reader)
    {
        int parentIdOrdinal = reader.GetOrdinal("parent_department_id");
        int parentNameOrdinal = reader.GetOrdinal("parent_name");

        return new DepartmentNode
        {
            Id = reader.GetDecimal(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Code = reader.GetString(reader.GetOrdinal("code")),
            ParentDepartmentId = reader.IsDBNull(parentIdOrdinal) ? null : reader.GetDecimal(parentIdOrdinal),
            ParentName = reader.IsDBNull(parentNameOrdinal) ? null : reader.GetString(parentNameOrdinal),
            Depth = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("depth"))),
            DirectHeadcount = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("direct_headcount"))),
            RollupHeadcount = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("rollup_headcount")))
        };
    }
}
