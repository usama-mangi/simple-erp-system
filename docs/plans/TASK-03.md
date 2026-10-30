# Plan: TASK-03 — department_managers feature

## Context and dependencies

Depends on TASK-01 (employee layer) — currently `testing` but verified in live use across
several sessions of fixes; TASK-06/TASK-05 remain unrelated. DB-driven lookups (TASK-02
phase 1) are in place: employee pickers also come from real data, never enum positions.

## Scope

Per-department management history, mirroring the employment_details pattern:

- `DepartmentManager` model: Id, DepartmentId, DepartmentName (join), ManagerId,
  ManagerName (subquery concatenation), StartDate, EndDate, CreatedAt, UpdatedAt.
- `IDepartmentManagersRepository`:
  - `GetAllByDepartmentAsync(departmentId)` — open row first, then history by start_date DESC,
    LEFT JOIN departments, manager name via scalar subquery against employees.
  - `GetByIdAsync(id)` — used by close validation.
  - `CreateAsync(DepartmentManager)` — FK/check mapping; ORA-00001 on
    `uq_dept_current_manager` surfaces as new `DuplicateCurrentManagerException`
    (constraint tree, folder EmployeeManager alongside DuplicateCurrentJob).
  - `CloseAsync(id, endDate)` — `UPDATE ... SET end_date WHERE id = :Id AND end_date IS NULL`;
    zero matches -> `NotFoundException` (same semantics as employment close).
  - DB permits two concurrent managers of one department (the unique index only binds
    one manager to one department); documented in code comment, no extra guard.
- Exceptions: `DuplicateCurrentManagerException(ex)` with FieldName "ManagerId";
  `DepartmentManagerCloseViewModel` mirrors the employment close VM.

## Web layer

- `DepartmentManagersController`:
  - `Index(departmentId)` — page model `DepartmentManagerIndexViewModel { Department, Managers }`;
    lookup titles via existing `ILookupRepository`/`IDepartmentRepository` + employees via
    `IEmployeeRepository.GetAllAsync()`.
  - `Create` GET/POST — ManagerId select (employees ordered by name), StartDate/EndDate
    (EndDate optional history backfill), antiforgery, PRG, typed field errors.
  - `Close` POST — inline form per open row in Index (same as employment history).
- Views: `Views/DepartmentManagers/Index.cshtml`, `Create.cshtml` (bootstrap/tag-helper
  form pattern, TempData alerts).
- Entry link: Department Index rows gain "Managers" (asp-controller DepartmentManagers,
  asp-route-departmentId).
- Program.cs DI registration.

## Verification

- dotnet build clean.
- Live checks: assign manager to Department; second open assignment for same manager hits
  DuplicateCurrentManagerException field error; close via inline form closes row; closed
  rows immutable (second close errors).

## Out of scope

- Cross-department manager overview, manager "works here" badge on employee details,
  decimal ids (TASK-05), department delete view fix (TASK-06).
