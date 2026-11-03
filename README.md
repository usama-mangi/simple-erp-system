# SmartERP

An ERP learning project focused on the HR domain: employees, departments, employment records, and department manager history against an Oracle database. The schema design is fully built out in Oracle; the ASP.NET Core web app implements it feature by feature.

![Entity relationship diagram](ERD.png)

## Structure

```
SmartERP.slnx
├── src/
│   ├── SmartERP.Web      # ASP.NET Core MVC front end
│   └── SmartERP.Data     # Data layer: repositories, typed exceptions, raw ADO.NET
└── db/                   # Oracle SQL scripts. Canonical schema + data live here;
                          # only numbered migrations are changes.
    ├── rebuild.sql       # Destructive dev reset: @drop @schema @procedures @triggers @seed
    ├── upgrade.sql       # Non-destructive upgrade: runs migrations/ in order
    ├── drop.sql          # Drop all objects for a clean rebuild
    ├── schema.sql        # Tables, constraints, unique indexes (canonical)
    ├── procedures.sql    # Stored procedures: sp_assign_manager (atomic end-then-insert manager assignment)
    ├── triggers.sql      # updated_at stamps + audit_log triggers
    ├── seed.sql          # Single dataset: lookups, ~260 employees, leave, documents fixtures
    └── migrations/       # Numbered, guarded, re-runnable changes for populated DBs
        ├── 001_positions.sql          # employment_details.job_title migrates onto positions
        ├── 002_leave.sql              # leave_types, leave_statuses, leave_requests, leave_balances
        ├── 003_department_hierarchy.sql # departments.parent_department_id + self-FK + no-self-parent check
        ├── 004_status_events.sql      # status_events (assignment status churn history)
        ├── 005_app_users_audit.sql    # app_users (1:1 employees) + audit_log
        └── 006_documents.sql          # document_types, documents, indexes, triggers
```

Fresh installs get everything from `schema.sql`; already-populated installs instead run `upgrade.sql` (or the one new migration) — both paths produce the same state. Dropping a table drops its identity sequence too, so ids restart at 1 per rebuild; anyone hard-coding lookup or row ids in code instead of reading them from queries will drift.

## Data model

Seventeen tables in two groups.

Lookup tables:

| Table | Columns |
|---|---|
| `genders` | `title` unique |
| `employment_types` | `title` unique |
| `employment_status` | `title` unique |
| `leave_types` | `title` unique: the leave kinds a request can be booked against |
| `leave_statuses` | `title` unique: Pending, Approved, Rejected, Cancelled. Pending seeds as id 1 |

Main tables:

| Table | What it stores |
|---|---|
| `employees` | Personal info: names, unique email and phone, date of birth, gender, address, and `is_deleted` for the soft-delete flag |
| `positions` | Position titles referenced by jobs; `employment_details` points at a position instead of carrying a free-text `job_title` |
| `departments` | Name, unique 3-letter code, location, established date, and nullable `parent_department_id` for the reporting line |
| `employment_details` | One job assignment: position, salary, start and end dates, plus FKs to employee, department, type, and status |
| `department_managers` | Management assignment: department, manager, start and end dates |
| `leave_requests` | One leave request: employee, type, status, start and end dates, optional reason, and `decided_at` once it leaves Pending |
| `leave_balances` | One row per employee, leave type and year: `entitlement_days` set from the app, `used_days` moved only by approving or cancelling a request |
| `document_types` | Document categories (contract, passport, certification…), seeded master data, `title` unique |
| `documents` | Per-employee document metadata: type, title, issue/expiry dates, note. Never stores file contents |

Design rules worth noting:

- All primary keys use `number generated always as identity`.
- Every table carrying real data has `created_at` (defaults to `systimestamp`) and `updated_at` (stamped by the triggers in `triggers.sql`).
- `salary >= 0` is enforced, and `end_date`, where present, must be later than `start_date`.
- Leave dates allow `end_date = start_date`: a single-day request is a normal request.
- `departments.parent_department_id` is a nullable self-FK: null means top level. `chk_dept_parent_not_self` blocks a row reporting to itself, and the app walks the ancestor chain so a department cannot report to one of its own sub-departments. Deleting a department that still has reports is blocked by the FK; there is no cascade.
- Department headcount is derived, never stored: open `employment_details` rows for non-deleted employees, counted per node and again across its whole subtree for the reporting structure page.
- Three function-based unique indexes prevent duplicate open rows:
  - `uq_det_current_job`: an employee can have only one `employment_details` row with a null `end_date`, so only one current job.
  - `uq_dept_current_manager`: a manager can head only one department at a time.
  - `uq_lr_open_request`: an employee can have only one Pending `leave_requests` row, so only one request waiting for a decision. The index compares `status_id` against `leave_statuses.id = 1`, which is why the seed inserts Pending first and fails loudly if anything else claims id 1.
- `leave_balances.used_days` is written by two paths only: approving a request adds its inclusive days, cancelling an approved request gives them back. `chk_bal_used` keeps it between 0 and `entitlement_days`.
- `documents` is metadata only: `issued_on`/`expires_at` optional but when both are set, `expires_at >= issued_on` (`chk_doc_dates`). Nothing references `documents`, so deleting a document has no cascade concerns; deleting an employee with documents is blocked by `fk_doc_employee` until the documents are removed (the purge flow removes them for you).
- Soft delete: `UPDATE employees SET is_deleted = 1` hides the row from app reads (every read filters `is_deleted = 0`); children stay intact. Deleting an employee together with their dependent rows is an explicit separate transaction in the data layer.
- Closed rows (with an `end_date`) are the history of assignments. Status transitions inside an
  open assignment are recorded in `status_events` (from, to, when, note); the final status
  stays on the assignment row, so existing reads remain unchanged.

## Relationships

```
employees 1───* employment_details *───1 departments
departments 1───* departments (parent_department_id)
employment_details *───1 positions
employees 1───* department_managers *───1 departments
employment_details *───1 employment_types
employment_details *───1 employment_status
employees *───1 genders
employees 1───* leave_requests *───1 leave_types
leave_requests *───1 leave_statuses
employees + leave_types 1───* leave_balances
employees 1───* documents *───1 document_types
```

The diagram above (generated with dbdiagram.io) shows the same model with column types.

## Running the database

Reset a dev instance from scratch: run `db/rebuild.sql` against the target instance. It drops every object, recreates the schema, loads the dataset, and finishes with row counts. It destroys data, use it on a database you are happy to reseed.

Upgrade an existing instance instead: run `db/upgrade.sql`. It applies the numbered migrations in `db/migrations/` in order and is safe to re-run.

## Running the web app

```bash
dotnet run --project src/SmartERP.Web
```

Requires the .NET SDK and:

1. `db/` run once against the instance (see above).
2. The Oracle connection string under `ConnectionStrings:Oracle`. It lives in no appsettings file on purpose; provide it either with user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Oracle" "User Id=...;Password=...;Data Source=..." --project src/SmartERP.Web
```

or with an environment variable `ConnectionStrings__Oracle`. Without one, startup fails in `OracleDbConnection` by design: no default credentials.

## What works

- Departments: full CRUD, with a reporting line set on create or update and a reporting-structure page that lists departments depth-first with the department each one reports to, its own open headcount, and the headcount across everything below it. The update form never offers the department itself or its descendants as a parent, and the database rejects a self-parent. A per-department manager-assignment page assigns an employee as manager and closes the assignment with an end date; a manager may head only one department at a time.
- Employees: full CRUD with soft delete plus a double-gated "delete with dependencies" purge, and a per-employee employment history page (record assignments, backfill closed history, close the open one; an employee holds only one open assignment).
- Dashboard: read-only statistics page (nav "Dashboard") built from the employment_details and department_managers closed rows. KPI tiles (headcount, monthly payroll, hires and exits with 12-month comparisons), six charts rendered with the bundled Chart.js copy in `wwwroot/lib/chart/` (workforce-size area, hires-vs-exits bars over trailing 24 months, exits by status, employment-type mix, payroll by department, min/max salary range per department), plus action lists: departments without a current manager, employees in probation beyond 90 days, and employees on leave or serving notice. Trend months derive from history rows, so soft-deleting an employee retroactively removes their share of every month.
- Documents: per-employee metadata vault (contract, passport, certifications…) with issue/expiry dates and a note. Documents are metadata rows only, no file storage; the files themselves are not on the server. A Documents page per employee lists them soonest-expiry first with a stamped status (VALID / DUE SOON / EXPIRED), add, edit and delete in place. The dashboard shows documents expiring within 30 days (including already-lapsed rows) in its action-lists; building the vault into the employee purge means deleting an employee with dependencies removes their documents too.
- Leave: request, decide and balance pages. An employee holds one Pending request at a time (enforced by `uq_lr_open_request`), a new request may not overlap approved leave, approving checks the year's entitlement first and then moves `used_days` in the same transaction, and cancelling an approved request gives the days back. Balances are set per employee, leave type and year from the balances page. The dashboard carries an "On leave today" list and an absence strip (days lost this month, requests waiting, people out today).
- Dropdown ids and titles come from the lookup tables themselves, not from code enums.
