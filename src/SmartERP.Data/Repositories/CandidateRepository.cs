using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class CandidateRepository(
    IDbConnectionFactory _dbConnection,
    IOracleExceptionHandler _oracleExceptionHandler
) : ICandidateRepository
{
    private const string SelectCandidateColumns = @"
        id, first_name, last_name, email, phone, resume_url, source,
        status_id, s.title as status_title,
        created_at, updated_at";
    private const string FromClause = @"FROM candidates c
        JOIN application_stages s ON s.id = c.status_id";

    public async Task<IEnumerable<Candidate>> GetAllCandidatesAsync()
    {
        string query = $"SELECT {SelectCandidateColumns} {FromClause}";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            using var reader = await command.ExecuteReaderAsync();

            var candidates = new List<Candidate>();
            while (await reader.ReadAsync())
            {
                candidates.Add(new Candidate
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Phone = reader.GetString(reader.GetOrdinal("phone")),
                    ResumeUrl = reader.IsDBNull(reader.GetOrdinal("resume_url")) ? null : reader.GetString(reader.GetOrdinal("resume_url")),
                    Source = reader.GetString(reader.GetOrdinal("source")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return candidates;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<IEnumerable<Candidate>> GetCandidatesByStatusAsync(decimal statusId)
    {
        string query = $"SELECT {SelectCandidateColumns} {FromClause} WHERE c.status_id = :StatusId";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p = new OracleParameter(":StatusId", OracleDbType.Decimal);
            p.Value = statusId;
            command.Parameters.Add(p);

            using var reader = await command.ExecuteReaderAsync();

            var candidates = new List<Candidate>();
            while (await reader.ReadAsync())
            {
                candidates.Add(new Candidate
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Phone = reader.GetString(reader.GetOrdinal("phone")),
                    ResumeUrl = reader.IsDBNull(reader.GetOrdinal("resume_url")) ? null : reader.GetString(reader.GetOrdinal("resume_url")),
                    Source = reader.GetString(reader.GetOrdinal("source")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return candidates;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<Candidate?> GetCandidateByIdAsync(decimal id)
    {
        string query = $"SELECT {SelectCandidateColumns} {FromClause} WHERE c.id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p = new OracleParameter(":Id", OracleDbType.Decimal);
            p.Value = id;
            command.Parameters.Add(p);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Candidate
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Phone = reader.GetString(reader.GetOrdinal("phone")),
                    ResumeUrl = reader.IsDBNull(reader.GetOrdinal("resume_url")) ? null : reader.GetString(reader.GetOrdinal("resume_url")),
                    Source = reader.GetString(reader.GetOrdinal("source")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };
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

    public async Task<Candidate?> GetCandidateByEmailAsync(string email)
    {
        string query = $"SELECT {SelectCandidateColumns} {FromClause} WHERE c.email = :Email";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p = new OracleParameter(":Email", OracleDbType.Varchar2);
            p.Value = email;
            command.Parameters.Add(p);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Candidate
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Phone = reader.GetString(reader.GetOrdinal("phone")),
                    ResumeUrl = reader.IsDBNull(reader.GetOrdinal("resume_url")) ? null : reader.GetString(reader.GetOrdinal("resume_url")),
                    Source = reader.GetString(reader.GetOrdinal("source")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };
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

    public async Task CreateCandidateAsync(Candidate candidate)
    {
        string query = @"
            INSERT INTO candidates
                (first_name, last_name, email, phone, resume_url, source, status_id, created_at, updated_at)
            VALUES
                (:FirstName, :LastName, :Email, :Phone, :ResumeUrl, :Source, :StatusId, :CreatedAt, :UpdatedAt)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p1 = new OracleParameter(":FirstName", OracleDbType.Varchar2); p1.Value = candidate.FirstName; command.Parameters.Add(p1);
            var p2 = new OracleParameter(":LastName", OracleDbType.Varchar2); p2.Value = candidate.LastName; command.Parameters.Add(p2);
            var p3 = new OracleParameter(":Email", OracleDbType.Varchar2); p3.Value = candidate.Email; command.Parameters.Add(p3);
            var p4 = new OracleParameter(":Phone", OracleDbType.Varchar2); p4.Value = candidate.Phone; command.Parameters.Add(p4);
            var p5 = new OracleParameter(":ResumeUrl", OracleDbType.Varchar2); p5.Value = candidate.ResumeUrl != null ? candidate.ResumeUrl : (object)DBNull.Value; command.Parameters.Add(p5);
            var p6 = new OracleParameter(":Source", OracleDbType.Varchar2); p6.Value = candidate.Source; command.Parameters.Add(p6);
            var p7 = new OracleParameter(":StatusId", OracleDbType.Decimal); p7.Value = candidate.StatusId; command.Parameters.Add(p7);
            var p8 = new OracleParameter(":CreatedAt", OracleDbType.Date); p8.Value = candidate.CreatedAt; command.Parameters.Add(p8);
            var p9 = new OracleParameter(":UpdatedAt", OracleDbType.Date); p9.Value = candidate.UpdatedAt.HasValue ? candidate.UpdatedAt.Value : (object)DBNull.Value; command.Parameters.Add(p9);

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

    public async Task UpdateCandidateAsync(Candidate candidate)
    {
        string query = @"
            UPDATE candidates
            SET first_name = :FirstName,
                last_name = :LastName,
                email = :Email,
                phone = :Phone,
                resume_url = :ResumeUrl,
                source = :Source,
                status_id = :StatusId,
                updated_at = :UpdatedAt
            WHERE id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p1 = new OracleParameter(":Id", OracleDbType.Decimal); p1.Value = candidate.Id; command.Parameters.Add(p1);
            var p2 = new OracleParameter(":FirstName", OracleDbType.Varchar2); p2.Value = candidate.FirstName; command.Parameters.Add(p2);
            var p3 = new OracleParameter(":LastName", OracleDbType.Varchar2); p3.Value = candidate.LastName; command.Parameters.Add(p3);
            var p4 = new OracleParameter(":Email", OracleDbType.Varchar2); p4.Value = candidate.Email; command.Parameters.Add(p4);
            var p5 = new OracleParameter(":Phone", OracleDbType.Varchar2); p5.Value = candidate.Phone; command.Parameters.Add(p5);
            var p6 = new OracleParameter(":ResumeUrl", OracleDbType.Varchar2); p6.Value = candidate.ResumeUrl != null ? candidate.ResumeUrl : (object)DBNull.Value; command.Parameters.Add(p6);
            var p7 = new OracleParameter(":Source", OracleDbType.Varchar2); p7.Value = candidate.Source; command.Parameters.Add(p7);
            var p8 = new OracleParameter(":StatusId", OracleDbType.Decimal); p8.Value = candidate.StatusId; command.Parameters.Add(p8);
            var p10 = new OracleParameter(":UpdatedAt", OracleDbType.Date); p10.Value = candidate.UpdatedAt.HasValue ? candidate.UpdatedAt.Value : (object)DBNull.Value; command.Parameters.Add(p10);

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
}
