using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.Exceptions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// The per-employee document vault. Rows carry metadata only (no file
// storage); the expiry watchlist lives in StatisticsRepository alongside the
// other dashboard queries.
public class DocumentRepository(
    IDbConnectionFactory _dbConnection,
    IOracleExceptionHandler _oracleExceptionHandler
) : IDocumentRepository
{
    private const string ForEmployeeQuery = @"
        SELECT d.id, d.employee_id, d.type_id, t.title AS type_title,
               d.title, d.issued_on, d.expires_at, d.note, d.created_at
          FROM documents d
          JOIN document_types t ON t.id = d.type_id
         WHERE d.employee_id = :Id
         ORDER BY d.expires_at ASC NULLS LAST, d.id";

    private const string ByIdQuery = @"
        SELECT d.id, d.employee_id, d.type_id, t.title AS type_title,
               d.title, d.issued_on, d.expires_at, d.note, d.created_at
          FROM documents d
          JOIN document_types t ON t.id = d.type_id
         WHERE d.id = :Id";

    private const string InsertQuery = @"
        INSERT INTO documents (employee_id, type_id, title, issued_on, expires_at, note)
        VALUES (:EmployeeId, :TypeId, :Title, :IssuedOn, :ExpiresAt, :Note)";

    private const string UpdateQuery = @"
        UPDATE documents
           SET type_id = :TypeId,
               title = :Title,
               issued_on = :IssuedOn,
               expires_at = :ExpiresAt,
               note = :Note
         WHERE id = :Id";

    private const string DeleteQuery = @"
        DELETE FROM documents
         WHERE id = :Id";

    public async Task<IReadOnlyList<Document>> GetForEmployeeAsync(decimal employeeId)
    {
        var documents = new List<Document>();

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(ForEmployeeQuery, (OracleConnection)connection);
            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Decimal) { Value = employeeId });

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                documents.Add(MapRow(reader));
            }

            return documents;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<Document?> GetByIdAsync(decimal id)
    {
        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(ByIdQuery, (OracleConnection)connection);
            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Decimal) { Value = id });

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapRow(reader) : null;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task CreateAsync(Document document)
    {
        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(InsertQuery, (OracleConnection)connection);
            command.BindByName = true;
            AddDocumentParameters(command, document);
            command.Parameters.Add(new OracleParameter(":EmployeeId", OracleDbType.Decimal) { Value = document.EmployeeId });

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleCreateExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    public async Task UpdateAsync(Document document)
    {
        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(UpdateQuery, (OracleConnection)connection);
            command.BindByName = true;
            AddDocumentParameters(command, document);
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Decimal) { Value = document.Id });

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

    public async Task DeleteAsync(decimal id)
    {
        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(DeleteQuery, (OracleConnection)connection);
            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Decimal) { Value = id });

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleDeleteExceptions(ex);
            throw new DatabaseAccessException(ex);
        }
    }

    private static void AddDocumentParameters(OracleCommand command, Document document)
    {
        command.Parameters.Add(new OracleParameter(":TypeId", OracleDbType.Decimal) { Value = document.TypeId });
        command.Parameters.Add(new OracleParameter(":Title", OracleDbType.Varchar2, document.Title, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter(":IssuedOn", OracleDbType.Date) { Value = (object?)document.IssuedOn ?? DBNull.Value });
        command.Parameters.Add(new OracleParameter(":ExpiresAt", OracleDbType.Date) { Value = (object?)document.ExpiresAt ?? DBNull.Value });
        command.Parameters.Add(new OracleParameter(":Note", OracleDbType.Varchar2) { Value = (object?)document.Note ?? DBNull.Value });
    }

    private static Document MapRow(OracleDataReader reader) => new()
    {
        Id = reader.GetDecimal(reader.GetOrdinal("id")),
        EmployeeId = reader.GetDecimal(reader.GetOrdinal("employee_id")),
        TypeId = reader.GetDecimal(reader.GetOrdinal("type_id")),
        TypeTitle = reader.GetString(reader.GetOrdinal("type_title")),
        Title = reader.GetString(reader.GetOrdinal("title")),
        IssuedOn = reader.IsDBNull(reader.GetOrdinal("issued_on")) ? null : reader.GetDateTime(reader.GetOrdinal("issued_on")),
        ExpiresAt = reader.IsDBNull(reader.GetOrdinal("expires_at")) ? null : reader.GetDateTime(reader.GetOrdinal("expires_at")),
        Note = reader.IsDBNull(reader.GetOrdinal("note")) ? null : reader.GetString(reader.GetOrdinal("note")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
    };
}
