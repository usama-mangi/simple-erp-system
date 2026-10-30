# Plan: TASK-09 — Step 8 spec adherence

User approved by directive ("make the changes to adhere to this spec").

## 1. Database

- `01_schema.sql`: add `create unique index uq_employees_email_lower on employees (lower(email))`.
- New `05_procedures.sql`: `sp_assign_manager(p_department_id, p_employee_id, p_start_date)` —
  UPDATE current open row (end_date = p_start_date, no-match harmless), then INSERT the new
  row. Atomic per PL/SQL call semantics. No COMMIT inside: caller owns the transaction
  (documented; exceptions propagate and roll back with the caller's tx).
- README `db/` block gains the procedure file + case-insensitive email note. Rebuild required.

## 2. Repository contracts renamed to the spec (data layer only)

- `IEmploymentDetailsRepository` -> `IEmploymentRepository` (EmploymentRepository):
  `GetByEmployeeAsync`, `GetCurrentAsync`, `CreateAsync`, `CreateReplacingCurrentAsync`,
  `EndCurrentAsync(employeeId, endDate)` (replaces row-id CloseAsync; inline close form
  switches to employee-scoped close), `TransferCurrentDepartmentAsync(employeeId, departmentId)`
  for the "Change department" action.
- `IDepartmentManagersRepository` -> `IDepartmentManagerRepository` (DepartmentManagerRepository):
  `GetCurrentByDepartmentAsync`, `GetHistoryAsync`, `AssignAsync` — AssignAsync calls
  `sp_assign_manager` with `CommandType.StoredProcedure` inside an explicit caller transaction
  (COMMIT/ROLLBACK owned by .NET, the spec's learning item). Old standalone CreateAsync
  path dissolves into AssignAsync.
- Async transaction prototype Per spec pattern in the four multi-statement flows
  (Employee Create/Update/DeleteWithDetails, Employment Replace): `await using var tx =
  await conn.BeginTransactionAsync()`, `command.Transaction = (OracleTransaction)tx`,
  `CommitAsync/RollbackAsync` in catch. Managers assign stays single-procedure-call.

## 3. App-level date validation

- Employment create: new start_date must be after the current row's start_date (checked
  against `GetCurrentAsync`), else inline ModelState error referencing the current start date.
  The per-row CHECK on the old row remains a final safety net.

## 4. Web composition

- Shared partial `Views/Shared/_EmploymentHistory.cshtml` (employment history table +
  inline close + current-badge), consumed by Employment/Index and Employee Details.
- Shared partial `Views/Shared/_DepartmentManagerHistory.cshtml` (successive current-manager
  highlight + history + inline close), consumed by DepartmentManagers/Index and Department Details.
- Employee Details page: personal info + history partial + `_EmploymentHistory` buttons:
  "Add job" (-> Employment Create) and "Change department" (POST with department select),
  per the spec composition.
- Department Details page: composed `DepartmentDetailsViewModel` (Department + manager
  history + inline Assign form with employee dropdown). Failed POST re-renders with the
  dropdown repopulated.
- Controller action VMs swapped to the correspondent Page models; external entry links
  unchanged.

## 5. State

- Backlog TASK-09 -> testing after build; memory entry on the transaction commit-ownership
  lesson; verification list follows the spec's bullets (replace flow uses current read,
  safety net fires when the end step is skipped, procedure call works through caller tx).

## Out of scope

- decimal id refactor (TASK-05), department delete view (TASK-06), tests.
