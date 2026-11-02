using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// Shared insert for status_events. Both employment and employee repositories
// record transitions, so the SQL and bind shape live in one place.
internal static class StatusEventWriter
{
    public const string InsertSql = @"
        INSERT INTO status_events (employee_id, from_status_id, to_status_id, changed_at, note)
        VALUES (:EmployeeId, :FromStatusId, :ToStatusId, :ChangedAt, :Note)";

    public static async Task InsertAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        double employeeId,
        double? fromStatusId,
        double toStatusId,
        DateTime changedAt,
        string? note)
    {
        using var command = new OracleCommand(InsertSql, connection)
        {
            BindByName = true,
            Transaction = transaction
        };
        command.Parameters.Add(":EmployeeId", OracleDbType.Double, employeeId, System.Data.ParameterDirection.Input);
        command.Parameters.Add(":FromStatusId", OracleDbType.Double,
            fromStatusId is { } v ? v : (object)DBNull.Value,
            System.Data.ParameterDirection.Input);
        command.Parameters.Add(":ToStatusId", OracleDbType.Double, toStatusId, System.Data.ParameterDirection.Input);
        command.Parameters.Add(":ChangedAt", OracleDbType.TimeStamp, changedAt, System.Data.ParameterDirection.Input);
        command.Parameters.Add(":Note", OracleDbType.Varchar2, (object?)note ?? DBNull.Value, System.Data.ParameterDirection.Input);
        await command.ExecuteNonQueryAsync();
    }
}