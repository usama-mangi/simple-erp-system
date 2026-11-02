using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Exceptions.Constraint.Leave;
using SmartERP.Data.Exceptions.Read;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// Leave requests and the balances their approval consumes. Two rules shape
// this file: every bind name is used exactly once per statement (no
// repeated placeholders to reason about), and every status transition that
// moves money on the balance runs in one transaction so a rejected consume
// rolls the decision back with it.
public class LeaveRepository(
    IDbConnectionFactory _dbConnection,
    OracleExceptionHandler _oracleExceptionHandler
) : ILeaveRepository
{
    // is_deleted = 0 rides along on the join: a soft-deleted employee's leave
    // disappears from reads the same way it does everywhere else.
    private const string SelectColumns = @"
        lr.id, lr.employee_id, e.first_name || ' ' || e.last_name as employee_name,
        lr.leave_type_id, lt.title as leave_type_title,
        lr.status_id, ls.title as status_title,
        lr.start_date, lr.end_date, lr.reason, lr.decided_at,
        lr.created_at, lr.updated_at";

    private const string FromClause = @"
        FROM
            leave_requests lr
        JOIN
            employees e ON e.id = lr.employee_id AND e.is_deleted = 0
        JOIN
            leave_types lt ON lt.id = lr.leave_type_id
        JOIN
            leave_statuses ls ON ls.id = lr.status_id";

    public async Task<IEnumerable<LeaveRequest>> GetRequestsAsync(double? employeeId, double? statusId)
    {
        // NVL(null-filter, column) = column is "no filter" without a second
        // query variant: a null argument leaves the comparison self-evidently
        // true, a real id narrows it.
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                Nvl(:EmployeeId, lr.employee_id) = lr.employee_id
              AND
                Nvl(:StatusId, lr.status_id) = lr.status_id
            ORDER BY
                CASE WHEN lr.decided_at IS NULL THEN 0 ELSE 1 END,
                lr.start_date DESC,
                lr.id DESC";

        try
        {
            var requests = new List<LeaveRequest>();
            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            using (var command = new OracleCommand(query, (OracleConnection)connection))
            {
                command.BindByName = true;
                command.Parameters.Add(new OracleParameter(":EmployeeId", OracleDbType.Double)
                {
                    Value = employeeId is { } id ? id : DBNull.Value
                });
                command.Parameters.Add(new OracleParameter(":StatusId", OracleDbType.Double)
                {
                    Value = statusId is { } status ? status : DBNull.Value
                });

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    requests.Add(MapToRequest(reader));
                }
            }

            return requests;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<LeaveRequest?> GetRequestByIdAsync(double id)
    {
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                lr.id = :Id";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":Id", OracleDbType.Double) { Value = id });

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToRequest(reader);
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

    public async Task<LeaveRequest?> GetOpenRequestAsync(double employeeId, LeaveStatusIds statuses)
    {
        // uq_lr_open_request makes this at most one row.
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                lr.employee_id = :EmployeeId
              AND
                lr.status_id = :PendingStatusId";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection);

            command.BindByName = true;
            command.Parameters.Add(new OracleParameter(":EmployeeId", OracleDbType.Double) { Value = employeeId });
            command.Parameters.Add(new OracleParameter(":PendingStatusId", OracleDbType.Double) { Value = statuses.Pending });

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToRequest(reader);
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

    public async Task<IEnumerable<LeaveRequest>> GetActiveRequestsAsync(double employeeId, LeaveStatusIds statuses)
    {
        // The rows that still occupy the calendar: an open request blocks a
        // second one outright, an approved one blocks a range that overlaps it.
        string query = $@"
            SELECT {SelectColumns} {FromClause}
            WHERE
                lr.employee_id = :EmployeeId
              AND
                lr.status_id IN (:PendingStatusId, :ApprovedStatusId)
            ORDER BY
                lr.start_date DESC";

        try
        {
            var requests = new List<LeaveRequest>();
            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            using (var command = new OracleCommand(query, (OracleConnection)connection))
            {
                command.BindByName = true;
                command.Parameters.Add(new OracleParameter(":EmployeeId", OracleDbType.Double) { Value = employeeId });
                command.Parameters.Add(new OracleParameter(":PendingStatusId", OracleDbType.Double) { Value = statuses.Pending });
                command.Parameters.Add(new OracleParameter(":ApprovedStatusId", OracleDbType.Double) { Value = statuses.Approved });

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    requests.Add(MapToRequest(reader));
                }
            }

            return requests;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task CreateRequestAsync(LeaveRequest request)
    {
        string query = @"
            INSERT INTO
                leave_requests (employee_id, leave_type_id, status_id, start_date, end_date, reason)
            VALUES
                (:EmployeeId, :LeaveTypeId, :StatusId, :StartDate, :EndDate, :Reason)";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            AddParameters(command, request);
            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [
                    new DuplicateOpenLeaveRequestException(ex),
                    new InvalidLeaveDatesException(ex)
                ])
                .HandleCreateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task ApproveAsync(LeaveRequest request, LeaveStatusIds statuses)
    {
        // Two statements, one transaction: the status flip claims the decision,
        // the balance consume pays for it. Either failing rolls back both, so
        // the request can never sit Approved against a balance that never moved.
        string approveQuery = @"
            UPDATE
                leave_requests
            SET
                status_id = :ApprovedStatusId,
                decided_at = systimestamp
            WHERE
                id = :Id
              AND
                status_id = :PendingStatusId";

        // The WHERE is written against the request's own columns instead of a
        // subquery, so no bind name has to repeat.
        string consumeQuery = @"
            UPDATE
                leave_balances
            SET
                used_days = used_days + :Days
            WHERE
                employee_id = :EmployeeId
              AND
                leave_type_id = :LeaveTypeId
              AND
                year = :Year";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                using (var approveCommand = new OracleCommand(approveQuery, (OracleConnection)connection))
                {
                    approveCommand.Transaction = (OracleTransaction)transaction;
                    approveCommand.BindByName = true;
                    approveCommand.Parameters.Add(":ApprovedStatusId", OracleDbType.Double, statuses.Approved, ParameterDirection.Input);
                    approveCommand.Parameters.Add(":Id", OracleDbType.Double, request.Id, ParameterDirection.Input);
                    approveCommand.Parameters.Add(":PendingStatusId", OracleDbType.Double, statuses.Pending, ParameterDirection.Input);

                    var approved = await approveCommand.ExecuteNonQueryAsync();

                    if (approved == 0)
                    {
                        // Already decided, or gone: never approve a stale row.
                        throw new NotFoundException(nameof(LeaveRequest), request.Id);
                    }
                }

                using (var consumeCommand = new OracleCommand(consumeQuery, (OracleConnection)connection))
                {
                    consumeCommand.Transaction = (OracleTransaction)transaction;
                    consumeCommand.BindByName = true;
                    consumeCommand.Parameters.Add(":Days", OracleDbType.Double, request.Days, ParameterDirection.Input);
                    consumeCommand.Parameters.Add(":EmployeeId", OracleDbType.Double, request.EmployeeId, ParameterDirection.Input);
                    consumeCommand.Parameters.Add(":LeaveTypeId", OracleDbType.Double, request.LeaveTypeId, ParameterDirection.Input);
                    consumeCommand.Parameters.Add(":Year", OracleDbType.Int32, request.StartDate.Year, ParameterDirection.Input);

                    var consumed = await consumeCommand.ExecuteNonQueryAsync();

                    if (consumed == 0)
                    {
                        // No entitlement row for that year: the controller
                        // checks first, this is the race that slipped past.
                        throw new InsufficientLeaveBalanceException(
                            new InvalidOperationException(
                                $"No leave_balances row for employee {request.EmployeeId}, type {request.LeaveTypeId}, year {request.StartDate.Year}."
                            )
                        );
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [new InsufficientLeaveBalanceException(ex)])
                .HandleUpdateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task RejectAsync(double requestId, LeaveStatusIds statuses)
    {
        // Rejection is a single statement: nothing was ever consumed, so the
        // balance is not involved.
        string query = @"
            UPDATE
                leave_requests
            SET
                status_id = :RejectedStatusId,
                decided_at = systimestamp
            WHERE
                id = :Id
              AND
                status_id = :PendingStatusId";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            command.Parameters.Add(":RejectedStatusId", OracleDbType.Double, statuses.Rejected, ParameterDirection.Input);
            command.Parameters.Add(":Id", OracleDbType.Double, requestId, ParameterDirection.Input);
            command.Parameters.Add(":PendingStatusId", OracleDbType.Double, statuses.Pending, ParameterDirection.Input);

            var rejected = await command.ExecuteNonQueryAsync();

            if (rejected == 0)
            {
                throw new NotFoundException(nameof(LeaveRequest), requestId);
            }
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleUpdateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task CancelAsync(LeaveRequest request, LeaveStatusIds statuses)
    {
        // Pending and Approved can both be cancelled; Rejected and Cancelled
        // are terminal, so the WHERE refuses them and a repeat click reports
        // the row as gone instead of double-refunding an approved request.
        string cancelQuery = @"
            UPDATE
                leave_requests
            SET
                status_id = :CancelledStatusId,
                decided_at = systimestamp
            WHERE
                id = :Id
              AND
                status_id IN (:PendingStatusId, :ApprovedStatusId)";

        string refundQuery = @"
            UPDATE
                leave_balances
            SET
                used_days = used_days - :Days
            WHERE
                employee_id = :EmployeeId
              AND
                leave_type_id = :LeaveTypeId
              AND
                year = :Year";

        // Only an approved request ever consumed days; a pending one refunds
        // nothing. The balance row was written by ApproveAsync with the same
        // key, so a missing row here means the data was edited underneath us.
        bool refund = request.StatusId == statuses.Approved;

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                using (var cancelCommand = new OracleCommand(cancelQuery, (OracleConnection)connection))
                {
                    cancelCommand.Transaction = (OracleTransaction)transaction;
                    cancelCommand.BindByName = true;
                    cancelCommand.Parameters.Add(":CancelledStatusId", OracleDbType.Double, statuses.Cancelled, ParameterDirection.Input);
                    cancelCommand.Parameters.Add(":Id", OracleDbType.Double, request.Id, ParameterDirection.Input);
                    cancelCommand.Parameters.Add(":PendingStatusId", OracleDbType.Double, statuses.Pending, ParameterDirection.Input);
                    cancelCommand.Parameters.Add(":ApprovedStatusId", OracleDbType.Double, statuses.Approved, ParameterDirection.Input);

                    var cancelled = await cancelCommand.ExecuteNonQueryAsync();

                    if (cancelled == 0)
                    {
                        throw new NotFoundException(nameof(LeaveRequest), request.Id);
                    }
                }

                if (refund)
                {
                    using var refundCommand = new OracleCommand(refundQuery, (OracleConnection)connection);
                    refundCommand.Transaction = (OracleTransaction)transaction;
                    refundCommand.BindByName = true;
                    refundCommand.Parameters.Add(":Days", OracleDbType.Double, request.Days, ParameterDirection.Input);
                    refundCommand.Parameters.Add(":EmployeeId", OracleDbType.Double, request.EmployeeId, ParameterDirection.Input);
                    refundCommand.Parameters.Add(":LeaveTypeId", OracleDbType.Double, request.LeaveTypeId, ParameterDirection.Input);
                    refundCommand.Parameters.Add(":Year", OracleDbType.Int32, request.StartDate.Year, ParameterDirection.Input);

                    await refundCommand.ExecuteNonQueryAsync();

                    // chk_bal_used (used_days >= 0) is the safety net: a
                    // refund larger than what was consumed raises ORA-02290
                    // and rolls the cancel back with it.
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [new InsufficientLeaveBalanceException(ex)])
                .HandleUpdateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<IEnumerable<LeaveBalance>> GetBalancesAsync(double employeeId, int year)
    {
        string query = @"
            SELECT
                lb.id, lb.employee_id, e.first_name || ' ' || e.last_name as employee_name,
                lb.leave_type_id, lt.title as leave_type_title,
                lb.year, lb.entitlement_days, lb.used_days,
                lb.created_at, lb.updated_at
            FROM
                leave_balances lb
            JOIN
                employees e ON e.id = lb.employee_id AND e.is_deleted = 0
            JOIN
                leave_types lt ON lt.id = lb.leave_type_id
            WHERE
                lb.employee_id = :EmployeeId
              AND
                lb.year = :Year
            ORDER BY
                lt.title";

        try
        {
            var balances = new List<LeaveBalance>();
            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            using (var command = new OracleCommand(query, (OracleConnection)connection))
            {
                command.BindByName = true;
                command.Parameters.Add(new OracleParameter(":EmployeeId", OracleDbType.Double) { Value = employeeId });
                command.Parameters.Add(new OracleParameter(":Year", OracleDbType.Int32) { Value = year });

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    balances.Add(MapToBalance(reader));
                }
            }

            return balances;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task UpsertBalanceAsync(LeaveBalance balance)
    {
        // The USING select carries the entitlement so each bind name appears
        // once: matched rows take the new allowance, unmatched rows are created
        // with used_days at 0.
        string query = @"
            MERGE INTO
                leave_balances lb
            USING (
                SELECT
                    :EmployeeId as employee_id,
                    :LeaveTypeId as leave_type_id,
                    :Year as year,
                    :EntitlementDays as entitlement_days
                FROM
                    dual
            ) s ON (
                lb.employee_id = s.employee_id
                AND lb.leave_type_id = s.leave_type_id
                AND lb.year = s.year
            )
            WHEN MATCHED THEN UPDATE
                SET lb.entitlement_days = s.entitlement_days
            WHEN NOT MATCHED THEN INSERT (
                employee_id,
                leave_type_id,
                year,
                entitlement_days,
                used_days
            ) VALUES (
                s.employee_id,
                s.leave_type_id,
                s.year,
                s.entitlement_days,
                0
            )";

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            command.Parameters.Add(":EmployeeId", OracleDbType.Double, balance.EmployeeId, ParameterDirection.Input);
            command.Parameters.Add(":LeaveTypeId", OracleDbType.Double, balance.LeaveTypeId, ParameterDirection.Input);
            command.Parameters.Add(":Year", OracleDbType.Int32, balance.Year, ParameterDirection.Input);
            command.Parameters.Add(":EntitlementDays", OracleDbType.Double, balance.EntitlementDays, ParameterDirection.Input);

            await command.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleConstraintExceptions(ex, [new InsufficientLeaveBalanceException(ex)])
                .HandleCreateExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    private static void AddParameters(OracleCommand command, LeaveRequest request)
    {
        command.Parameters.Add(":EmployeeId", OracleDbType.Double, request.EmployeeId, ParameterDirection.Input);
        command.Parameters.Add(":LeaveTypeId", OracleDbType.Double, request.LeaveTypeId, ParameterDirection.Input);
        command.Parameters.Add(":StatusId", OracleDbType.Double, request.StatusId, ParameterDirection.Input);
        command.Parameters.Add(":StartDate", OracleDbType.Date, request.StartDate, ParameterDirection.Input);
        command.Parameters.Add(":EndDate", OracleDbType.Date, request.EndDate, ParameterDirection.Input);
        command.Parameters.Add(
            ":Reason",
            OracleDbType.Varchar2,
            string.IsNullOrWhiteSpace(request.Reason) ? DBNull.Value : request.Reason,
            ParameterDirection.Input
        );
    }

    private static LeaveRequest MapToRequest(OracleDataReader reader)
    {
        int decidedAtOrdinal = reader.GetOrdinal("decided_at");
        int updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new LeaveRequest
        {
            Id = reader.GetDouble(reader.GetOrdinal("id")),
            EmployeeId = reader.GetDouble(reader.GetOrdinal("employee_id")),
            EmployeeName = reader.GetString(reader.GetOrdinal("employee_name")),
            LeaveTypeId = reader.GetDouble(reader.GetOrdinal("leave_type_id")),
            LeaveTypeTitle = reader.GetString(reader.GetOrdinal("leave_type_title")),
            StatusId = reader.GetDouble(reader.GetOrdinal("status_id")),
            StatusTitle = reader.GetString(reader.GetOrdinal("status_title")),
            StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
            EndDate = reader.GetDateTime(reader.GetOrdinal("end_date")),
            Reason = reader.IsDBNull(reader.GetOrdinal("reason"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("reason")),
            DecidedAt = reader.IsDBNull(decidedAtOrdinal) ? null : reader.GetDateTime(decidedAtOrdinal),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? default : reader.GetDateTime(updatedAtOrdinal)
        };
    }

    private static LeaveBalance MapToBalance(OracleDataReader reader)
    {
        int updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new LeaveBalance
        {
            Id = reader.GetDouble(reader.GetOrdinal("id")),
            EmployeeId = reader.GetDouble(reader.GetOrdinal("employee_id")),
            EmployeeName = reader.GetString(reader.GetOrdinal("employee_name")),
            LeaveTypeId = reader.GetDouble(reader.GetOrdinal("leave_type_id")),
            LeaveTypeTitle = reader.GetString(reader.GetOrdinal("leave_type_title")),
            Year = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("year"))),
            EntitlementDays = reader.GetDouble(reader.GetOrdinal("entitlement_days")),
            UsedDays = reader.GetDouble(reader.GetOrdinal("used_days")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? default : reader.GetDateTime(updatedAtOrdinal)
        };
    }
}
