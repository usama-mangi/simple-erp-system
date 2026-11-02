using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class AppUserRepository(IDbConnectionFactory db, OracleExceptionHandler handler) : IAppUserRepository
{
    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        const string q = @"SELECT id, employee_id, username, password_hash, role, is_active, created_at, updated_at FROM app_users WHERE username = :U";
        try
        {
            using var c = await db.CreateOpenConnectionAsync();
            using var cmd = new OracleCommand(q, (OracleConnection)c) { BindByName = true };
            cmd.Parameters.Add(":U", OracleDbType.Varchar2, username, System.Data.ParameterDirection.Input);
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync()) return Map(r);
            return null;
        }
        catch (OracleException ex)
        {
            handler.HandlePermissionExceptions(ex).HandleReadExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<AppUser?> GetByIdAsync(double id)
    {
        const string q = @"SELECT id, employee_id, username, password_hash, role, is_active, created_at, updated_at FROM app_users WHERE id = :Id";
        try
        {
            using var c = await db.CreateOpenConnectionAsync();
            using var cmd = new OracleCommand(q, (OracleConnection)c) { BindByName = true };
            cmd.Parameters.Add(":Id", OracleDbType.Double, id, System.Data.ParameterDirection.Input);
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync()) return Map(r);
            return null;
        }
        catch (OracleException ex)
        {
            handler.HandlePermissionExceptions(ex).HandleReadExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task CreateAsync(AppUser user)
    {
        const string q = @"INSERT INTO app_users (employee_id, username, password_hash, role, is_active) VALUES (:E, :U, :P, :R, :A) RETURNING id INTO :NewId";
        try
        {
            using var c = await db.CreateOpenConnectionAsync();
            using var cmd = new OracleCommand(q, (OracleConnection)c) { BindByName = true };
            cmd.Parameters.Add(":E", OracleDbType.Double, user.EmployeeId, System.Data.ParameterDirection.Input);
            cmd.Parameters.Add(":U", OracleDbType.Varchar2, user.Username, System.Data.ParameterDirection.Input);
            cmd.Parameters.Add(":P", OracleDbType.Varchar2, user.PasswordHash, System.Data.ParameterDirection.Input);
            cmd.Parameters.Add(":R", OracleDbType.Varchar2, user.Role, System.Data.ParameterDirection.Input);
            cmd.Parameters.Add(":A", OracleDbType.Int32, user.IsActive ? 1 : 0, System.Data.ParameterDirection.Input);
            var outp = cmd.Parameters.Add(new OracleParameter(":NewId", OracleDbType.Double) { Direction = System.Data.ParameterDirection.Output });
            await cmd.ExecuteNonQueryAsync();
            if (outp.Value is OracleDecimal od && !od.IsNull) user.Id = (double)od;
        }
        catch (OracleException ex)
        {
            handler.HandlePermissionExceptions(ex).HandleCreateExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    private static AppUser Map(OracleDataReader r)
    {
        int upd = r.GetOrdinal("updated_at");
        return new AppUser
        {
            Id = r.GetDouble(r.GetOrdinal("id")),
            EmployeeId = r.GetDouble(r.GetOrdinal("employee_id")),
            Username = r.GetString(r.GetOrdinal("username")),
            PasswordHash = r.GetString(r.GetOrdinal("password_hash")),
            Role = r.GetString(r.GetOrdinal("role")),
            IsActive = r.GetInt32(r.GetOrdinal("is_active")) == 1,
            CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
            UpdatedAt = r.IsDBNull(upd) ? default : r.GetDateTime(upd)
        };
    }
}