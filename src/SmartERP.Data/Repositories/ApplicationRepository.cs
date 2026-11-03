using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class ApplicationRepository(
    IDbConnectionFactory _dbConnection,
    IOracleExceptionHandler _oracleExceptionHandler
) : IApplicationRepository
{
    private const string SelectApplicationColumns = @"
        a.id, a.candidate_id, c.first_name || ' ' || c.last_name as candidate_name,
        a.requisition_id, r.title as requisition_title,
        a.stage_id, s.title as stage_title,
        a.status_id, st.title as status_title,
        a.applied_date, a.last_updated_date, a.notes,
        a.created_at, a.updated_at";
    private const string FromClause = @"
        FROM applications a
        JOIN candidates c ON c.id = a.candidate_id
        JOIN requisitions r ON r.id = a.requisition_id
        JOIN application_stages s ON s.id = a.stage_id
        JOIN application_stages st ON st.id = a.status_id";

    public async Task<IEnumerable<Application>> GetApplicationsByCandidateAsync(decimal candidateId)
    {
        string query = $"SELECT {SelectApplicationColumns} {FromClause} WHERE a.candidate_id = :CandidateId ORDER BY a.applied_date DESC";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p = new OracleParameter(":CandidateId", OracleDbType.Decimal);
            p.Value = candidateId;
            command.Parameters.Add(p);

            using var reader = await command.ExecuteReaderAsync();

            var applications = new List<Application>();
            while (await reader.ReadAsync())
            {
                applications.Add(new Application
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    CandidateId = reader.GetDecimal(reader.GetOrdinal("candidate_id")),
                    CandidateName = reader.GetString(reader.GetOrdinal("candidate_name")),
                    RequisitionId = reader.GetDecimal(reader.GetOrdinal("requisition_id")),
                    RequisitionTitle = reader.GetString(reader.GetOrdinal("requisition_title")),
                    StageId = reader.GetDecimal(reader.GetOrdinal("stage_id")),
                    StageTitle = reader.GetString(reader.GetOrdinal("stage_title")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    AppliedDate = reader.GetDateTime(reader.GetOrdinal("applied_date")),
                    LastUpdatedDate = reader.IsDBNull(reader.GetOrdinal("last_updated_date")) ? default : reader.GetDateTime(reader.GetOrdinal("last_updated_date")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return applications;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<IEnumerable<Application>> GetApplicationsByRequisitionAsync(decimal requisitionId)
    {
        string query = $"SELECT {SelectApplicationColumns} {FromClause} WHERE a.requisition_id = :RequisitionId ORDER BY a.applied_date DESC";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p = new OracleParameter(":RequisitionId", OracleDbType.Decimal);
            p.Value = requisitionId;
            command.Parameters.Add(p);

            using var reader = await command.ExecuteReaderAsync();

            var applications = new List<Application>();
            while (await reader.ReadAsync())
            {
                applications.Add(new Application
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    CandidateId = reader.GetDecimal(reader.GetOrdinal("candidate_id")),
                    CandidateName = reader.GetString(reader.GetOrdinal("candidate_name")),
                    RequisitionId = reader.GetDecimal(reader.GetOrdinal("requisition_id")),
                    RequisitionTitle = reader.GetString(reader.GetOrdinal("requisition_title")),
                    StageId = reader.GetDecimal(reader.GetOrdinal("stage_id")),
                    StageTitle = reader.GetString(reader.GetOrdinal("stage_title")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    AppliedDate = reader.GetDateTime(reader.GetOrdinal("applied_date")),
                    LastUpdatedDate = reader.IsDBNull(reader.GetOrdinal("last_updated_date")) ? default : reader.GetDateTime(reader.GetOrdinal("last_updated_date")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return applications;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<IEnumerable<Application>> GetApplicationsByStageAsync(decimal stageId)
    {
        string query = $"SELECT {SelectApplicationColumns} {FromClause} WHERE a.stage_id = :StageId ORDER BY a.applied_date DESC";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p = new OracleParameter(":StageId", OracleDbType.Decimal);
            p.Value = stageId;
            command.Parameters.Add(p);

            using var reader = await command.ExecuteReaderAsync();

            var applications = new List<Application>();
            while (await reader.ReadAsync())
            {
                applications.Add(new Application
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    CandidateId = reader.GetDecimal(reader.GetOrdinal("candidate_id")),
                    CandidateName = reader.GetString(reader.GetOrdinal("candidate_name")),
                    RequisitionId = reader.GetDecimal(reader.GetOrdinal("requisition_id")),
                    RequisitionTitle = reader.GetString(reader.GetOrdinal("requisition_title")),
                    StageId = reader.GetDecimal(reader.GetOrdinal("stage_id")),
                    StageTitle = reader.GetString(reader.GetOrdinal("stage_title")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    AppliedDate = reader.GetDateTime(reader.GetOrdinal("applied_date")),
                    LastUpdatedDate = reader.IsDBNull(reader.GetOrdinal("last_updated_date")) ? default : reader.GetDateTime(reader.GetOrdinal("last_updated_date")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return applications;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<Application?> GetApplicationByIdAsync(decimal id)
    {
        string query = $"SELECT {SelectApplicationColumns} {FromClause} WHERE a.id = :Id";

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
                return new Application
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    CandidateId = reader.GetDecimal(reader.GetOrdinal("candidate_id")),
                    CandidateName = reader.GetString(reader.GetOrdinal("candidate_name")),
                    RequisitionId = reader.GetDecimal(reader.GetOrdinal("requisition_id")),
                    RequisitionTitle = reader.GetString(reader.GetOrdinal("requisition_title")),
                    StageId = reader.GetDecimal(reader.GetOrdinal("stage_id")),
                    StageTitle = reader.GetString(reader.GetOrdinal("stage_title")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    AppliedDate = reader.GetDateTime(reader.GetOrdinal("applied_date")),
                    LastUpdatedDate = reader.IsDBNull(reader.GetOrdinal("last_updated_date")) ? default : reader.GetDateTime(reader.GetOrdinal("last_updated_date")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
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

    public async Task<Application?> GetApplicationByCandidateAndRequisitionAsync(decimal candidateId, decimal requisitionId)
    {
        string query = $"SELECT {SelectApplicationColumns} {FromClause} WHERE a.candidate_id = :CandidateId AND a.requisition_id = :RequisitionId";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p1 = new OracleParameter(":CandidateId", OracleDbType.Decimal); p1.Value = candidateId; command.Parameters.Add(p1);
            var p2 = new OracleParameter(":RequisitionId", OracleDbType.Decimal); p2.Value = requisitionId; command.Parameters.Add(p2);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Application
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    CandidateId = reader.GetDecimal(reader.GetOrdinal("candidate_id")),
                    CandidateName = reader.GetString(reader.GetOrdinal("candidate_name")),
                    RequisitionId = reader.GetDecimal(reader.GetOrdinal("requisition_id")),
                    RequisitionTitle = reader.GetString(reader.GetOrdinal("requisition_title")),
                    StageId = reader.GetDecimal(reader.GetOrdinal("stage_id")),
                    StageTitle = reader.GetString(reader.GetOrdinal("stage_title")),
                    StatusId = reader.GetDecimal(reader.GetOrdinal("status_id")),
                    StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
                    AppliedDate = reader.GetDateTime(reader.GetOrdinal("applied_date")),
                    LastUpdatedDate = reader.IsDBNull(reader.GetOrdinal("last_updated_date")) ? default : reader.GetDateTime(reader.GetOrdinal("last_updated_date")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
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

    public async Task CreateApplicationAsync(Application application)
    {
        string query = @"
            INSERT INTO applications
                (candidate_id, requisition_id, stage_id, status_id, applied_date, last_updated_date, notes, created_at, updated_at)
            VALUES
                (:CandidateId, :RequisitionId, :StageId, :StatusId, :AppliedDate, :LastUpdatedDate, :Notes, :CreatedAt, :UpdatedAt)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p1 = new OracleParameter(":CandidateId", OracleDbType.Decimal); p1.Value = application.CandidateId; command.Parameters.Add(p1);
            var p2 = new OracleParameter(":RequisitionId", OracleDbType.Decimal); p2.Value = application.RequisitionId; command.Parameters.Add(p2);
            var p3 = new OracleParameter(":StageId", OracleDbType.Decimal); p3.Value = application.StageId; command.Parameters.Add(p3);
            var p4 = new OracleParameter(":StatusId", OracleDbType.Decimal); p4.Value = application.StatusId; command.Parameters.Add(p4);
            var p5 = new OracleParameter(":AppliedDate", OracleDbType.Date); p5.Value = application.AppliedDate; command.Parameters.Add(p5);
            var p6 = new OracleParameter(":LastUpdatedDate", OracleDbType.Date); p6.Value = application.LastUpdatedDate.HasValue ? application.LastUpdatedDate.Value : (object)DBNull.Value; command.Parameters.Add(p6);
            var p7 = new OracleParameter(":Notes", OracleDbType.Varchar2); p7.Value = application.Notes != null ? (object)application.Notes : DBNull.Value; command.Parameters.Add(p7);
            var p8 = new OracleParameter(":CreatedAt", OracleDbType.Date); p8.Value = application.CreatedAt; command.Parameters.Add(p8);
            var p9 = new OracleParameter(":UpdatedAt", OracleDbType.Date); p9.Value = application.UpdatedAt.HasValue ? application.UpdatedAt.Value : (object)DBNull.Value; command.Parameters.Add(p9);

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

    public async Task UpdateApplicationAsync(Application application)
    {
        string query = @"
            UPDATE applications
            SET stage_id = :StageId,
                status_id = :StatusId,
                applied_date = :AppliedDate,
                last_updated_date = :LastUpdatedDate,
                notes = :Notes,
                updated_at = :UpdatedAt
            WHERE id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            var p1 = new OracleParameter(":Id", OracleDbType.Decimal); p1.Value = application.Id; command.Parameters.Add(p1);
            var p2 = new OracleParameter(":StageId", OracleDbType.Decimal); p2.Value = application.StageId; command.Parameters.Add(p2);
            var p3 = new OracleParameter(":StatusId", OracleDbType.Decimal); p3.Value = application.StatusId; command.Parameters.Add(p3);
            var p4 = new OracleParameter(":AppliedDate", OracleDbType.Date); p4.Value = application.AppliedDate; command.Parameters.Add(p4);
            var p5 = new OracleParameter(":LastUpdatedDate", OracleDbType.Date); p5.Value = application.LastUpdatedDate.HasValue ? application.LastUpdatedDate.Value : (object)DBNull.Value; command.Parameters.Add(p5);
            var p6 = new OracleParameter(":Notes", OracleDbType.Varchar2); p6.Value = application.Notes != null ? (object)application.Notes : DBNull.Value; command.Parameters.Add(p6);
            var p7 = new OracleParameter(":UpdatedAt", OracleDbType.Date); p6.Value = application.UpdatedAt.HasValue ? application.UpdatedAt.Value : (object)DBNull.Value; command.Parameters.Add(p6);

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
