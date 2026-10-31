using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public class StatisticsRepository(
    IDbConnectionFactory _dbConnection,
    OracleExceptionHandler _oracleExceptionHandler
) : IStatisticsRepository
{
    // Trend window: 24 month-end points. Long enough that the seeded career
    // history (2019+) is visible, aligned with the 12+12 flow comparison.
    private const int TrendMonths = 24;

    // The year-over-year comparison half of each flow pair.
    private const int ComparisonMonths = 12;

    // An employee still on 'Probation' after this many days lands on the
    // watchlist.
    private const int ProbationDaysBeforeAlert = 90;

    private const string ProbationStatus = "Probation";

    private static readonly string[] PendingExitStatuses = ["On Leave", "Notice Period"];

    private const string WorkforceTrendPointsQuery = @"
        SELECT
            t.mth            AS month,
            COUNT(DISTINCT ed.employee_id) AS headcount,
            NVL(SUM(CASE WHEN e.id IS NOT NULL THEN ed.salary END), 0) AS payroll
        FROM (
            SELECT ADD_MONTHS(TRUNC(SYSDATE, 'MM'), 1 - LEVEL) AS mth
            FROM dual
            CONNECT BY LEVEL <= :WindowMonths
        ) t
        LEFT JOIN employment_details ed
            ON  ed.start_date <= t.mth
            AND NVL(ed.end_date, t.mth) >= t.mth
        LEFT JOIN employees e
            ON  e.id = ed.employee_id
            AND e.is_deleted = 0
        GROUP BY
            t.mth
        ORDER BY
            t.mth";

    private const string MonthlyFlowsQuery = @"
        SELECT
            t.mth AS month,
            COUNT(CASE WHEN e.id IS NOT NULL AND TRUNC(ed.start_date, 'MM') = t.mth THEN 1 END) AS hires,
            COUNT(CASE WHEN e.id IS NOT NULL AND TRUNC(ed.end_date, 'MM') = t.mth   THEN 1 END) AS exits
        FROM (
            SELECT ADD_MONTHS(TRUNC(SYSDATE, 'MM'), 1 - LEVEL) AS mth
            FROM dual
            CONNECT BY LEVEL <= :WindowMonths
        ) t
        LEFT JOIN employment_details ed
            ON  TRUNC(ed.start_date, 'MM') = t.mth
            OR  TRUNC(ed.end_date, 'MM')   = t.mth
        LEFT JOIN employees e
            ON  e.id = ed.employee_id
            AND e.is_deleted = 0
        GROUP BY
            t.mth
        ORDER BY
            t.mth";

    private const string ExitsByStatusQuery = @"
        SELECT
            es.title AS status,
            COUNT(*) AS count
        FROM
            employment_details ed
        JOIN
            employees e ON e.id = ed.employee_id AND e.is_deleted = 0
        JOIN
            employment_status es ON es.id = ed.employment_status_id
        WHERE
            ed.end_date IS NOT NULL
            AND ed.end_date >= ADD_MONTHS(TRUNC(SYSDATE, 'MM'), -:WindowMonths)
        GROUP BY
            es.title
        ORDER BY
            count DESC, es.title";

    private const string TypeShareQuery = @"
        SELECT
            et.title AS type,
            COUNT(*) AS count
        FROM
            employment_details ed
        JOIN
            employees e ON e.id = ed.employee_id AND e.is_deleted = 0
        JOIN
            employment_types et ON et.id = ed.employment_type_id
        WHERE
            ed.end_date IS NULL
        GROUP BY
            et.title
        ORDER BY
            count DESC, et.title";

    private const string PayrollByDepartmentQuery = @"
        SELECT
            d.code          AS code,
            d.name          AS name,
            COUNT(*)        AS headcount,
            SUM(ed.salary)  AS total_salary,
            AVG(ed.salary)  AS average_salary,
            MIN(ed.salary)  AS minimum_salary,
            MAX(ed.salary)  AS maximum_salary
        FROM
            employment_details ed
        JOIN
            departments d ON d.id = ed.department_id
        JOIN
            employees e ON e.id = ed.employee_id AND e.is_deleted = 0
        WHERE
            ed.end_date IS NULL
        GROUP BY
            d.code, d.name
        ORDER BY
            total_salary DESC";

    private const string WatchlistQuery = @"
        SELECT
            e.first_name || ' ' || e.last_name AS employee_name,
            ed.job_title                       AS job_title,
            d.name                             AS department_name,
            ed.salary                          AS salary,
            ed.start_date                      AS start_date,
            TRUNC(SYSDATE) - ed.start_date     AS days_in_status
        FROM
            employment_details ed
        JOIN
            employees e ON e.id = ed.employee_id AND e.is_deleted = 0
        JOIN
            departments d ON d.id = ed.department_id
        JOIN
            employment_status es ON es.id = ed.employment_status_id
        WHERE
            ed.end_date IS NULL
            AND es.title = :StatusTitle
            AND ed.start_date <= TRUNC(SYSDATE) - :ProbationDays
        ORDER BY
            ed.start_date";

    // Two statuses, both possible for one open row, so the IN list is a fixed
    // bind pair; the statuses themselves come from the code, not user input.
    private const string PendingExitsQuery = @"
        SELECT
            e.first_name || ' ' || e.last_name AS employee_name,
            ed.job_title                       AS job_title,
            d.name                             AS department_name,
            ed.salary                          AS salary,
            ed.start_date                      AS start_date,
            TRUNC(SYSDATE) - ed.start_date     AS days_in_status
        FROM
            employment_details ed
        JOIN
            employees e ON e.id = ed.employee_id AND e.is_deleted = 0
        JOIN
            departments d ON d.id = ed.department_id
        JOIN
            employment_status es ON es.id = ed.employment_status_id
        WHERE
            ed.end_date IS NULL
            AND es.title IN (:StatusOne, :StatusTwo)
        ORDER BY
            es.title, ed.start_date";

    private const string UnmanagedDepartmentsQuery = @"
        SELECT
            d.code       AS code,
            d.name       AS name,
            d.location   AS location,
            d.established AS established
        FROM
            departments d
        LEFT JOIN
            department_managers dm ON dm.department_id = d.id AND dm.end_date IS NULL
        WHERE
            dm.id IS NULL
        ORDER BY
            d.name";

    public async Task<WorkforceTrend> GetWorkforceTrendAsync()
    {
        try
        {
            var points = new List<MonthlyWorkforcePoint>();
            var flows = new List<MonthlyFlowPoint>();

            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            {
                using (var command = new OracleCommand(WorkforceTrendPointsQuery, (OracleConnection)connection))
                {
                    command.BindByName = true;
                    AddWindowMonths(command, TrendMonths);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        points.Add(new MonthlyWorkforcePoint(
                            reader.GetDateTime(reader.GetOrdinal("month")),
                            Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("headcount"))),
                            reader.GetDouble(reader.GetOrdinal("payroll"))
                        ));
                    }
                }

                using (var command = new OracleCommand(MonthlyFlowsQuery, (OracleConnection)connection))
                {
                    command.BindByName = true;
                    AddWindowMonths(command, TrendMonths);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        flows.Add(new MonthlyFlowPoint(
                            reader.GetDateTime(reader.GetOrdinal("month")),
                            Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("hires"))),
                            Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("exits")))
                        ));
                    }
                }
            }

            return new WorkforceTrend(points, flows);
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<PeriodSummary> GetPeriodSummaryAsync()
    {
        try
        {
            var exitsByStatus = new List<ExitStatusCount>();
            var typeShares = new List<EmploymentTypeShare>();

            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            {
                using (var command = new OracleCommand(ExitsByStatusQuery, (OracleConnection)connection))
                {
                    command.BindByName = true;
                    AddWindowMonths(command, ComparisonMonths);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        exitsByStatus.Add(new ExitStatusCount(
                            reader.GetString(reader.GetOrdinal("status")),
                            Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("count")))
                        ));
                    }
                }

                using (var command = new OracleCommand(TypeShareQuery, (OracleConnection)connection))
                {
                    command.BindByName = true;

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        typeShares.Add(new EmploymentTypeShare(
                            reader.GetString(reader.GetOrdinal("type")),
                            Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("count")))
                        ));
                    }
                }
            }

            return new PeriodSummary(exitsByStatus, typeShares);
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<IEnumerable<DepartmentPayroll>> GetPayrollByDepartmentAsync()
    {
        string query = PayrollByDepartmentQuery;

        try
        {
            using var connection = await _dbConnection.CreateOpenConnectionAsync();
            using var command = new OracleCommand(query, (OracleConnection)connection)
            {
                BindByName = true
            };

            using var reader = await command.ExecuteReaderAsync();

            var payrolls = new List<DepartmentPayroll>();
            while (await reader.ReadAsync())
            {
                payrolls.Add(new DepartmentPayroll(
                    reader.GetString(reader.GetOrdinal("code")),
                    reader.GetString(reader.GetOrdinal("name")),
                    Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("headcount"))),
                    reader.GetDouble(reader.GetOrdinal("total_salary")),
                    reader.GetDouble(reader.GetOrdinal("average_salary")),
                    reader.GetDouble(reader.GetOrdinal("minimum_salary")),
                    reader.GetDouble(reader.GetOrdinal("maximum_salary"))
                ));
            }

            return payrolls;
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    public async Task<Watchlists> GetWatchlistsAsync()
    {
        try
        {
            var probation = new List<WatchlistEntry>();
            var pendingExits = new List<WatchlistEntry>();
            var unmanaged = new List<UnmanagedDepartment>();

            using (var connection = await _dbConnection.CreateOpenConnectionAsync())
            {
                using (var command = new OracleCommand(WatchlistQuery, (OracleConnection)connection))
                {
                    command.BindByName = true;
                    command.Parameters.Add("StatusTitle", OracleDbType.Varchar2, ProbationStatus, ParameterDirection.Input);
                    command.Parameters.Add("ProbationDays", OracleDbType.Int32, ProbationDaysBeforeAlert, ParameterDirection.Input);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        probation.Add(MapWatchlistEntry(reader));
                    }
                }

                using (var command = new OracleCommand(PendingExitsQuery, (OracleConnection)connection))
                {
                    command.BindByName = true;
                    command.Parameters.Add("StatusOne", OracleDbType.Varchar2, PendingExitStatuses[0], ParameterDirection.Input);
                    command.Parameters.Add("StatusTwo", OracleDbType.Varchar2, PendingExitStatuses[1], ParameterDirection.Input);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        pendingExits.Add(MapWatchlistEntry(reader));
                    }
                }

                using (var command = new OracleCommand(UnmanagedDepartmentsQuery, (OracleConnection)connection))
                {
                    command.BindByName = true;

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        unmanaged.Add(new UnmanagedDepartment(
                            reader.GetString(reader.GetOrdinal("code")),
                            reader.GetString(reader.GetOrdinal("name")),
                            reader.GetString(reader.GetOrdinal("location")),
                            reader.GetDateTime(reader.GetOrdinal("established"))
                        ));
                    }
                }
            }

            return new Watchlists(probation, pendingExits, unmanaged);
        }
        catch (OracleException ex)
        {
            _oracleExceptionHandler
                .HandlePermissionExceptions(ex)
                .HandleReadExceptions(ex);

            throw new DatabaseAccessException(ex);
        }
    }

    private static void AddWindowMonths(OracleCommand command, int months)
        => command.Parameters.Add("WindowMonths", OracleDbType.Int32, months, ParameterDirection.Input);

    private static WatchlistEntry MapWatchlistEntry(OracleDataReader reader)
    {
        int startDateOrdinal = reader.GetOrdinal("start_date");

        return new WatchlistEntry(
            reader.GetString(reader.GetOrdinal("employee_name")),
            reader.GetString(reader.GetOrdinal("job_title")),
            reader.GetString(reader.GetOrdinal("department_name")),
            reader.GetDouble(reader.GetOrdinal("salary")),
            reader.GetDateTime(startDateOrdinal),
            Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("days_in_status")))
        );
    }
}
