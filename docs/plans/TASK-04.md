# Plan: TASK-04 — sync docs with the actual codebase

Drift found while auditing; each bullet is one commit-sized unit:

1. README `db/` structure block: claims `04_triggers.sql` and "no 03 exists";
   reality is `03_triggers.sql` (real triggers) + `04_test.sql` (empty placeholder).
2. README data-model notes: missing `employees.is_deleted` (soft delete), missing
   the rebuild requirement after schema changes.
3. README running-the-web-app: connection string lives in
   `ConnectionStrings:Oracle` (user secrets / env var), nothing in appsettings —
   document how to provide it, or every startup dies in OracleDbConnection.
4. README running/web feature list now stale: employment history pages and
   department managers pages exist.
5. AGENTS.md architecture section: repositories list outdated (still only
   IEmployeeRepository/IDepartmentRepository).

## Acceptance

- README matches disk 1:1 for db scripts and describes the soft-delete contract.
- Connection string setup documented.
- AGENTS.md repository/controller inventory matches src.

## Out of scope

- README生成 styling, CI docs, architecture redesign.
