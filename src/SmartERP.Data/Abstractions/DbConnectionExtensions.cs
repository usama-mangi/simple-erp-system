using System.Data;
using System.Data.Common;

namespace SmartERP.Data.Abstractions;

// Async transaction pattern helper: the factory hands out IDbConnection, which
// predates the async API surface on DbConnection. OracleConnection is a
// DbConnection, so callers always get the async path; the sync fallback keeps
// the helper honest for non-Db providers.
public static class DbConnectionExtensions
{
    public static ValueTask<DbTransaction> BeginTransactionAsync(
        this IDbConnection connection,
        CancellationToken cancellationToken = default
    ) => connection is DbConnection dbConnection
        ? dbConnection.BeginTransactionAsync(cancellationToken)
        : new ValueTask<DbTransaction>((DbTransaction)connection.BeginTransaction());
}
