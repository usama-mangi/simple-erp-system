using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class ReviewRepository(IDbConnectionFactory _dbConnection, IOracleExceptionHandler _oracleExceptionHandler) : IReviewRepository
{
    private const string SelectReviewCycleColumns = @"
        rc.id, rc.title, rc.description, rc.start_date, rc.end_date, rc.is_active,
        rc.created_at, rc.updated_at";

    private const string SelectReviewColumns = @"
        mr.id, mr.employee_id, e.first_name || ' ' || e.last_name as employee_name,
        mr.reviewer_id, r.first_name || ' ' || r.last_name as reviewer_name,
        mr.review_cycle_id, rc.title as cycle_title,
        mr.rating, mr.comments, mr.review_date,
        mr.created_at, mr.updated_at";

    private const string FromReviewCycleClause = @"
        FROM review_cycles rc";
    private const string FromReviewClause = @"
        FROM manager_reviews mr
        JOIN employees e ON e.id = mr.employee_id AND e.is_deleted = 0
        JOIN employees r ON r.id = mr.reviewer_id AND r.is_deleted = 0
        JOIN review_cycles rc ON rc.id = mr.review_cycle_id";

    public async Task<IEnumerable<ReviewCycle>> GetCyclesAsync(bool? isActive = null)
    {
        string query = isActive.HasValue
            ? $"SELECT {SelectReviewCycleColumns} {FromReviewCycleClause} WHERE rc.is_active = :IsActive ORDER BY rc.title"
            : $"SELECT {SelectReviewCycleColumns} {FromReviewCycleClause} ORDER BY rc.title";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            if (isActive.HasValue)
            {
                command.Parameters.Add(new OracleParameter(":IsActive", OracleDbType.Decimal)
                {
                    Value = isActive.Value ? 1 : 0
                });
            }

            using var reader = await command.ExecuteReaderAsync();

            var cycles = new List<ReviewCycle>();
            while (await reader.ReadAsync())
            {
                cycles.Add(new ReviewCycle
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
                    EndDate = reader.GetDateTime(reader.GetOrdinal("end_date")),
                    IsActive = reader.GetInt32(reader.GetOrdinal("is_active")) == 1,
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return cycles;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<ReviewCycle?> GetCycleByIdAsync(decimal id)
    {
        string query = $"SELECT {SelectReviewCycleColumns} {FromReviewCycleClause} WHERE rc.id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Decimal)
            {
                Value = id
            });

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ReviewCycle
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
                    EndDate = reader.GetDateTime(reader.GetOrdinal("end_date")),
                    IsActive = reader.GetInt32(reader.GetOrdinal("is_active")) == 1,
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

    public async Task<IEnumerable<ManagerReview>> GetReviewsByEmployeeAsync(decimal employeeId, decimal? cycleId = null)
    {
        string where = cycleId.HasValue
            ? "WHERE mr.review_cycle_id = :CycleId AND mr.employee_id = :EmployeeId"
            : "WHERE mr.employee_id = :EmployeeId";

        string query = $"SELECT {SelectReviewColumns} {FromReviewClause} {where} ORDER BY mr.review_date DESC, mr.id DESC";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.Parameters.Add(new OracleParameter(":EmployeeId", OracleDbType.Decimal)
            {
                Value = employeeId
            });

            if (cycleId.HasValue)
            {
                command.Parameters.Add(new OracleParameter(":CycleId", OracleDbType.Decimal)
                {
                    Value = cycleId.Value
                });
            }

            using var reader = await command.ExecuteReaderAsync();

            var reviews = new List<ManagerReview>();
            while (await reader.ReadAsync())
            {
                reviews.Add(new ManagerReview
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    EmployeeId = reader.GetDecimal(reader.GetOrdinal("employee_id")),
                    EmployeeName = reader.GetString(reader.GetOrdinal("employee_name")),
                    ReviewerId = reader.GetDecimal(reader.GetOrdinal("reviewer_id")),
                    ReviewerName = reader.GetString(reader.GetOrdinal("reviewer_name")),
                    ReviewCycleId = reader.GetDecimal(reader.GetOrdinal("review_cycle_id")),
                    ReviewCycleTitle = reader.GetString(reader.GetOrdinal("cycle_title")),
                    Rating = reader.GetDecimal(reader.GetOrdinal("rating")),
                    Comments = reader.IsDBNull(reader.GetOrdinal("comments")) ? string.Empty : reader.GetString(reader.GetOrdinal("comments")),
                    ReviewDate = reader.GetDateTime(reader.GetOrdinal("review_date")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return reviews;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<IEnumerable<ManagerReview>> GetReviewsByCycleAsync(decimal cycleId)
    {
        string query = $"SELECT {SelectReviewColumns} {FromReviewClause} WHERE mr.review_cycle_id = :CycleId ORDER BY mr.review_date DESC, mr.id DESC";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.Parameters.Add(new OracleParameter(":CycleId", OracleDbType.Decimal)
            {
                Value = cycleId
            });

            using var reader = await command.ExecuteReaderAsync();

            var reviews = new List<ManagerReview>();
            while (await reader.ReadAsync())
            {
                reviews.Add(new ManagerReview
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    EmployeeId = reader.GetDecimal(reader.GetOrdinal("employee_id")),
                    EmployeeName = reader.GetString(reader.GetOrdinal("employee_name")),
                    ReviewerId = reader.GetDecimal(reader.GetOrdinal("reviewer_id")),
                    ReviewerName = reader.GetString(reader.GetOrdinal("reviewer_name")),
                    ReviewCycleId = reader.GetDecimal(reader.GetOrdinal("review_cycle_id")),
                    ReviewCycleTitle = reader.GetString(reader.GetOrdinal("cycle_title")),
                    Rating = reader.GetDecimal(reader.GetOrdinal("rating")),
                    Comments = reader.IsDBNull(reader.GetOrdinal("comments")) ? string.Empty : reader.GetString(reader.GetOrdinal("comments")),
                    ReviewDate = reader.GetDateTime(reader.GetOrdinal("review_date")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? default : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return reviews;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<ManagerReview?> GetReviewByIdAsync(decimal id)
    {
        string query = $"SELECT {SelectReviewColumns} {FromReviewClause} WHERE mr.id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Decimal)
            {
                Value = id
            });

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ManagerReview
                {
                    Id = reader.GetDecimal(reader.GetOrdinal("id")),
                    EmployeeId = reader.GetDecimal(reader.GetOrdinal("employee_id")),
                    EmployeeName = reader.GetString(reader.GetOrdinal("employee_name")),
                    ReviewerId = reader.GetDecimal(reader.GetOrdinal("reviewer_id")),
                    ReviewerName = reader.GetString(reader.GetOrdinal("reviewer_name")),
                    ReviewCycleId = reader.GetDecimal(reader.GetOrdinal("review_cycle_id")),
                    ReviewCycleTitle = reader.GetString(reader.GetOrdinal("cycle_title")),
                    Rating = reader.GetDecimal(reader.GetOrdinal("rating")),
                    Comments = reader.IsDBNull(reader.GetOrdinal("comments")) ? string.Empty : reader.GetString(reader.GetOrdinal("comments")),
                    ReviewDate = reader.GetDateTime(reader.GetOrdinal("review_date")),
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

    public async Task CreateReviewAsync(ManagerReview review)
    {
        string query = @"
            INSERT INTO manager_reviews
                (employee_id, reviewer_id, review_cycle_id, rating, comments, review_date, created_at, updated_at)
            VALUES
                (:EmployeeId, :ReviewerId, :ReviewCycleId, :Rating, :Comments, :ReviewDate, :CreatedAt, :UpdatedAt)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.Parameters.Add(new OracleParameter(":EmployeeId", OracleDbType.Decimal, review.EmployeeId, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":ReviewerId", OracleDbType.Decimal, review.ReviewerId, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":ReviewCycleId", OracleDbType.Decimal, review.ReviewCycleId, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":Rating", OracleDbType.Decimal, review.Rating, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":Comments", OracleDbType.Varchar2, string.IsNullOrWhiteSpace(review.Comments) ? (object)DBNull.Value : review.Comments, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":ReviewDate", OracleDbType.Date, review.ReviewDate, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":CreatedAt", OracleDbType.TimeStamp, review.CreatedAt, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":UpdatedAt", OracleDbType.TimeStamp, review.UpdatedAt ?? (object)DBNull.Value, ParameterDirection.Input));

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

    public async Task UpdateReviewAsync(ManagerReview review)
    {
        string query = @"
            UPDATE manager_reviews
            SET rating = :Rating,
                comments = :Comments,
                review_date = :ReviewDate,
                updated_at = :UpdatedAt
            WHERE id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Decimal, review.Id, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":Rating", OracleDbType.Decimal, review.Rating, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":Comments", OracleDbType.Varchar2, string.IsNullOrWhiteSpace(review.Comments) ? (object)DBNull.Value : review.Comments, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":ReviewDate", OracleDbType.Date, review.ReviewDate, ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter(":UpdatedAt", OracleDbType.TimeStamp, review.UpdatedAt ?? (object)DBNull.Value, ParameterDirection.Input));

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
