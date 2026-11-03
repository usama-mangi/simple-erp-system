# Plan: TASK-06 — Department delete: confirmation view, GET action, gated destructive submit

## Objective

Finish the department delete flow. Today the only entry point is a POST form on
Department/Index with a native `confirm()`, and `DepartmentController.Delete` POST
returns `View(model)` when ModelState is invalid — a 500, because no
`Views/Department/Delete.cshtml` exists and no GET action is defined. Add the
confirmation step the rest of the app already uses (mirror Employee Delete):
GET Delete renders a record sheet of what would be removed, the destructive
submit is enabled only when the delete can actually succeed, the index row links
to it, and destructive POST stays Admin-only like employee delete.

## Dependencies

- None (TASK-05 closed)

## Current state

- `DepartmentController.Delete(DepartmentDeleteViewModel)` POST exists and works on the
  happy path: `GetByIdAsync` (404 if missing) → `DeleteAsync` (hard `DELETE FROM departments`,
  ORA-2292 → `ChildFoundException` → TempData error → redirect Index).
- Invalid ModelState → `View(model)` → view not found → 500.
- Schema: no `ON DELETE` clauses anywhere, so ANY dependent row blocks the delete with
  ORA-2292: `employment_details` (department_id, open or closed), `department_managers`
  (department_id), child departments (`parent_department_id`).
- Employee delete pattern to mirror: GET builds `EmployeeDeleteViewModel` with
  DisplayName + `HasDependenciesAsync` (single scalar over dual); POST is
  `[Authorize(Roles = "Admin")]`, redirects on invalid ModelState.
- `IDepartmentRepository.GetByIdAsync` LEFT JOINs the parent, so `ParentName` is populated.

## Target state

- `/Department/Delete?id=` renders a confirmation page in register style: breadcrumb,
  h1, record sheet (Code / Name / Reports to / Location / Established / Dependent records),
  and one destructive action.
- When the department has dependent rows the destructive button is disabled and the
  page says exactly what blocks the delete; the submit posts only when nothing blocks it
  (re-checked server-side by the DB; ORA-2292 remains the race backstop).
- Department/Index row action links to the confirmation page instead of POSTing blindly.
- Destructive POST is Admin-only, matching employee delete.

## Files to modify

| Action | Path | What |
|--------|------|------|
| MODIFY | `src/SmartERP.Data/Repositories/IDepartmentRepository.cs` | add `Task<bool> HasDependenciesAsync(decimal id)` |
| MODIFY | `src/SmartERP.Data/Repositories/DepartmentRepository.cs` | implement: one scalar over dual summing COUNT(*) from employment_details + department_managers + departments(parent); `HandleReadExceptions` + `DatabaseAccessException`, mirroring EmployeeRepository |
| MODIFY | `src/SmartERP.Web/Models/DepartmentDeleteViewModel.cs` | add `DisplayName`, `Code`, `Location`, `Established` (DateTime), `ParentName`, `HasDependencies` |
| MODIFY | `src/SmartERP.Web/Controllers/DepartmentController.cs` | add `[HttpGet] Delete(decimal id)`: GetByIdAsync (null → NotFound), map model, catch → TempData + redirect Index; POST: add `[Authorize(Roles = "Admin")]`, invalid ModelState → `RedirectToAction(nameof(Index))` (drop `View(model)`) |
| CREATE | `src/SmartERP.Web/Views/Department/Delete.cshtml` | register-styled confirmation: register-back breadcrumb, h1 "Delete department", subtitle statement, meta `RECORD #…`, `record-sheet` (Code/Name/Reports to/Location/Established/Dependent records), blocked state (`register-warn` + disabled `btn-stamp--reject` with plain-language blocker list) or enabled state (native `confirm()` + submit) |
| MODIFY | `src/SmartERP.Web/Views/Department/Index.cshtml` | row Delete becomes `asp-action="Delete" asp-route-id=` link (class `btn btn-stamp btn-stamp--reject`); drop the inline POST form and its `confirm()` |

## Implementation steps

1. **Repository.** Interface method + implementation (single scalar, `dept_count`-style);
   verify with `grep -n HasDependenciesAsync` and a build of the Data project.
2. **Model.** Extend `DepartmentDeleteViewModel`; keep `Id` `[Required]`.
3. **Controller.** GET action with try/catch mirroring employee GET Delete; POST changes
   (Admin role; invalid ModelState redirect). Build stays green.
4. **Delete.cshtml.** Write the view mirroring `Views/Employee/Delete.cshtml` structure and
   the register system (record-sheet, register-warn, breadcrumb, decision hierarchy):
   blocked state when `HasDependencies` (disabled submit, exact blocker list: employment
   assignments / management history / sub-departments), enabled state otherwise
   (native confirm: "Delete this department? It is removed from the register permanently.").
   Success/error TempData render the same as the other register pages.
5. **Index row.** Swap the POST form for the GET link.
6. **Runtime verification.** Rebuild, restart app, authenticate, then:
   - GET `/Department/Delete?id=<blocked dept>` → 200, destructive button disabled,
     blocker copy rendered (the seeded IT department has assignments);
   - find or confirm an unblocked department (no assignments/managers/children) and check
     the enabled state renders with the confirm;
   - POST with a missing/invalid Id → 302 redirect to Index (no 500);
   - unauthenticated GET → redirected to Login;
   - both list pages and Details still render (no route regressions).
7. Full gate: `dotnet build` + click-through; route to audit in review mode.

## Out of scope

- Deleting departments with dependencies (cascade or "delete with dependencies"): departments
  have no soft-delete and business data blocks it; reassign/close first is the honest flow.
- Employee delete flow, purge flow, audit_log wiring (unchanged).
- Any change to `ChildFoundException` handling (kept as the race backstop).
- role-based UI hiding on the index (link stays visible to all roles; POST is Admin-gated).

## Definition of done

- [ ] `GET /Department/Delete?id=` renders 200 for an existing department; 404 for a missing one.
- [ ] With dependent rows: destructive button disabled and blockers listed; without: enabled
      with native confirm; submit POSTs and (on success) department is gone and Index shows
      the seal; a forced ORA-2292 still lands on the TempData message, not a 500.
- [ ] Invalid POST (missing Id) redirects to Index instead of erroring.
- [ ] No new Bootstrap component classes; page passes the same register-system checks as the
      other in-scope screens; `dotnet build` passes with 0 errors.

## Verification criteria

- [ ] Every DoD checkbox evidenced by HTTP result or rendered markup.
- [ ] Build passes; audit (review mode) pass recommendation.

## Rationale

Mirror the employee flow exactly where it fits, because it is the settled pattern
(GET confirm page → POST with Admin gate → TempData seal). Unlike employees, departments
have no soft-delete fallback and no purge path, so a blocked delete is terminal — hence
the disabled submit with the blocker list instead of an override. The index becomes a link
to the confirmation page so the destructive step is never one misclick away, and the
`confirm()` moves from the list into the record page where the user has the full context.
`HasDependenciesAsync` is computed at load only to drive the button state; the database
remains the authority (no reliance on stale pre-checks).
