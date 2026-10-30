# Plan: TASK-07 — employee soft delete + destructive dependencies purge

Approved decisions (from user): soft-deleted rows hidden everywhere; gate = checkbox
+ native confirm; main button stays soft delete; destructive button only when
dependencies exist.

## Schema (`db/01_schema.sql`, plus seed sanity in `02_seed.sql`)

- `employees` gains `is_deleted number(1) default 0 not null` right before created_at.
- No new triggers; the existing updated_at trigger already stamps the soft-delete update.
- Rebuild note: scripts are numbered rebuild-from-scratch; no migration needed.
- Seed inserts omit the column and fall back to the default 0.

## Data layer

- `DeleteAsync(double id)` → soft delete:
  `UPDATE employees SET is_deleted = 1 WHERE id = :Id`
  catch chain: permission + update exceptions.
- New `DeleteWithDetailsAsync(double id)` → single transaction, hard delete:
  delete from `employment_details` where `employee_id = :Id`, then
  `department_managers` where `manager_id = :Id`, then `employees` where `id = :Id`.
  Permission mapping; ChildFoundException cannot occur because children go first.
- New `HasDependenciesAsync(double id)` → bool:
  one scalar query over dual summing two count subqueries
  (employment_details by employee_id + department_managers by manager_id).

## Read contract

- `GetAllAsync` and `GetByIdAsync` add `WHERE is_deleted = 0` /
  `AND e.is_deleted = 0` so soft-deleted rows vanish from list and details.

## Web layer

- `IEmployeeRepository`: add `HasDependenciesAsync`, `DeleteWithDetailsAsync`.
- `EmployeeDeleteViewModel`: add `HasDependencies` (bool, hidden) alongside Id/DisplayName.
- `EmployeeController.Delete` GET: load employee, set `HasDependencies` from
  repository bool, pass to view.
- `EmployeeController.Delete` POST: branch on model.HasDependencies →
  `DeleteWithDetailsAsync` else `DeleteAsync`; success/error TempData unchanged.
- `Views/Employee/Delete.cshtml`:
  - always shows the soft-delete primary button;
  - when `Model.HasDependencies`: red warning line "The employee has dependent
    records." + destructive "Delete With Dependencies" button disabled until the
    "I understand this permanently removes the employee and all dependent
    records" checkbox is ticked, plus native confirm() on that form submit;
    submits `HasDependencies = true` via its own hidden input.

## Verification

- `dotnet build` clean.
- Live check by user: soft delete hides row; dependent employee shows warning +
  second button; purge flow removes employee and reindex shows them gone.

## What is NOT in scope

- Restore-undelete UI, decimal id refactor (TASK-05), department flows (TASK-03/06).
