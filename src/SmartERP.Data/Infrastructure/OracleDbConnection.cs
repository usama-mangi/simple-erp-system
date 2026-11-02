using System.Data;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;

namespace SmartERP.Data.Infrastructure;

public sealed class OracleDbConnection(IConfiguration configuration, IConnectionContext context) : IDbConnectionFactory
{
    private readonly string? _connectionString = configuration.GetConnectionString("Oracle")
            ?? throw new NullReferenceException("Connection string not set");

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(context.ClientIdentifier))
        {
            using var cmd = new OracleCommand("BEGIN DBMS_SESSION.SET_IDENTIFIER(:u); END;", connection);
            cmd.Parameters.Add(":u", OracleDbType.Varchar2, context.ClientIdentifier, System.Data.ParameterDirection.Input);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        return connection;
    }
}