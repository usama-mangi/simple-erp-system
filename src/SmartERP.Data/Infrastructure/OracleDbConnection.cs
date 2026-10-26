using System.Data;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;

namespace SmartERP.Data.Infrastructure;

public sealed class OracleDbConnection(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string? _connectionString = configuration.GetConnectionString("Oracle")
            ?? throw new NullReferenceException("Connection string not set");

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}