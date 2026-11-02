# AGENTS.md

## What

SmartERP is an HR-focused ERP learning project. It manages employees, departments, job assignments, and department manager history against an Oracle database, exposed through an ASP.NET Core MVC web app. Audience: one developer sharpening design, data-layer, and Oracle skills. Core workflows: browse departments and employees, create/update them, record a job assignment (employment_details), close it with an end date, assign a department manager, and run leave: request it, decide it, and track per-year balances. The Oracle schema in `db/` is fully built out; the web app is catching up to it.

## Why

A training ground for working with raw Oracle from .NET: identity columns, function-based unique indexes, check constraints, and ORA error codes mapped to typed exceptions. An ORM would hide the layer the developer wants to learn, so it is deliberately absent. The pain it solves for the developer: turning a designed ERD into a working full-stack app, constraint by constraint.

## Standard

Learning project, correctness over elegance. Every inherited constraint from the Oracle schema must be honored somewhere in the app: unique email/phone, unique department name/code, one current job per employee, one current manager per department, salary >= 0, end_date > start_date, one Pending leave request per employee, leave end_date >= start_date, and used leave days between 0 and the entitlement. A feature is done when the controller, views, and repository paths all work against a real Oracle instance. Partial solutions and stubs are failures.

## Non-Negotiables

- All SQL is parameterized with bind variables. No string-built SQL, ever.
- OracleException goes through `OracleExceptionHandler`; it converts ORA codes to the typed exceptions in `SmartERP.Data/Exceptions`. Repositories never leak raw OracleException to the web layer.
- Controllers catch repository exceptions and surface them as `ModelState` errors or `TempData` messages, matching the pattern already in `DepartmentController`.
- POST actions use `[ValidateAntiForgeryToken]`.
- Match the codebase style: primary constructors, repositories take `IDbConnectionFactory` and `OracleExceptionHandler`, async methods with `Async` suffix.
- New tables need a `db/NN_*.sql` script follow-up, numbered, and run in filename order.
- README must be updated when the db script layout or feature scope changes.

## Architecture

- **Solution**: `SmartERP.slnx`, .NET 10.
- **SmartERP.Web**: ASP.NET Core MVC. Controllers + Razor views + view models in `Models/`, custom validation attributes in `Validations/`. Entry point `Program.cs` wires DI.
- **SmartERP.Data**: class library. `Infrastructure/OracleDbConnection` implements `IDbConnectionFactory` and opens ODP.NET connections from the `Oracle` connection string. `Repositories/` holds one interface + implementation per aggregate or concern: `IDepartmentRepository`, `IEmployeeRepository`, `IEmploymentRepository`, `IDepartmentManagerRepository`, `ILookupRepository` (db-driven dropdown ids/titles, including leave types and leave statuses), `ILeaveRepository`, `IStatisticsRepository`, all with hand-written parameterized SQL. `ExceptionHandlers/OracleExceptionHandler` maps ORA codes. `Exceptions/` is a typed exception tree grouped by operation (Create/Read/Update/Delete/Constraint/Permission).
- **Database**: Oracle, twelve tables: three lookups (`genders`, `employment_types`, `employment_status`), `positions` (position titles consumed by employment forms via `ILookupRepository.GetPositionsAsync`, no free-text job_title), `employees` (with an `is_deleted` soft-delete flag), `departments` (with a nullable `parent_department_id` self-FK, `chk_dept_parent_not_self`, and app-level cycle checks), `employment_details`, `department_managers`, plus the leave domain: `leave_types` and `leave_statuses` lookups, `leave_requests`, and `leave_balances` (entitlement/used per employee, type and year). History is closed rows with `end_date`, no history tables. One Pending request per employee is enforced by the function-based index `uq_lr_open_request`, which binds `status_id` to `leave_statuses.id = 1`; the seed inserts Pending first and fails loudly if it does not land on id 1. Rebuild scripts live under `db/`; schema migrations for populated databases are numbered follow-up scripts there too (`06_positions.sql`, `07_leave.sql`, `08_department_hierarchy.sql`).
- **Data flow**: Browser -> MVC controller -> repository (parameterized SQL over ODP.NET, one connection per operation; cross-statement work runs in a local transaction) -> Oracle. Exceptions flow back as typed exceptions caught by controllers and rendered as `ModelState` or `TempData` messages.
- **Soft delete contract**: employee deletion writes `is_deleted = 1`; reads filter it out. A destructive "delete with dependencies" run purges employment_details, then department_managers, then leave_requests and leave_balances, then the employee, inside one transaction.
- **No auth yet, no tests yet.** Both are explicit gaps, not omissions. Ids are `double` today; a decimal refactor is tracked as TASK-05.
