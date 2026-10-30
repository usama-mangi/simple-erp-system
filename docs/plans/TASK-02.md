# Plan: TASK-02 — employment_details feature + lookup-driven dropdowns

## Phase 1 (fix active bug): stop deriving dropdown ids from code

Repro chain: Update POST writes an enum-position gender id (1..3) while the DB's
genders rows hold different identity values -> FK_EMP_GENDER rejects -> ParentNotFoundException.

- New `LookupItem(double Id, string Title)` in SmartERP.Data.Models.
- New `ILookupRepository`: `GetGendersAsync`, `GetEmploymentTypesAsync`,
  `GetEmploymentStatusesAsync` -> `IEnumerable<LookupItem>`; each a parameterless
  SELECT id, title ORDER BY id with permission/read exception mapping.
- `EmployeeController`: inject `ILookupRepository`; `PopulateSelectLists` builds
  SelectList from DB values for genders/types/statuses (departments unchanged).
- `EmployeeCreateViewModel` / `EmployeeUpdateViewModel`: GenderId range becomes
  presence-based `Range(1, double.MaxValue)`; same for EmploymentTypeId /
  EmploymentStatusId literals (1,5)/(1,8) — conditional IValidatableObject checks
  already handle the details-only cases.
- Program.cs DI registration.
- Enum models (Gender/EmploymentType/EmploymentStatus) stay for display casting
  in Details view: replace `((Gender)Model.GenderId)` with stored titles via a
  lightweight id->title lookup passed by the controller, or display raw Title
  fetched in Details. Decision: extend GetByIdAsync? No — display casts removed;
  Details view receives a SelectList dictionary `ViewData["LookupTitles"]`?
 SIMPLER: Details cshtml drops enum casts; the controller passes `GetAllAsync`
  lookups via ViewData and the view maps id->title inline. Keep data layer untouched.

## Phase 2: employment_details repository + UI

- `IEmploymentDetailsRepository`: list by employee, create (uq_det_current_job
  guard via typed exception), update open row, close with end_date.
- Views under Views/Employment: Index per employee, Create, close action.
- Depends on Phase 1 lookups for dropdowns.

## Deferred

- Department managers UI (TASK-03), decimal refactor (TASK-05).
