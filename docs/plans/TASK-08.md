# Plan: TASK-08 — replace-current flow + sanitized user-facing errors

## Part A: constraint matching fixed at the root

`HandleConstraintExceptions` matches by `FieldName` text inside the ORA message.
Function-based indexes (SYS_NC00012$) don't print the field, so nothing matched and
the raw message leaked. Change every typed constraint exception to carry the
**schema constraint name** it represents; the handler now matches by that:

- DuplicateEmailException -> UQ_EMPLOYEES_EMAIL
- DuplicatePhoneException -> UQ_EMPLOYEES_PHONE
- DuplicateDepartmentsName -> UQ_DEPARTMENTS_NAME
- DuplicateDepartmentsCode -> UQ_DEPARTMENTS_CODE
- DuplicateCurrentJobException -> UQ_DET_CURRENT_JOB
- DuplicateCurrentManagerException -> UQ_DEPT_CURRENT_MANAGER
- InvalidSalaryException -> CHK_DET_SALARY
- EndDateOlderThanStartDate -> CHK_DET_END_DATE / CHK_MGR_END_DATE (shared "END_DATE")

`ConstraintException` gains a `constraintName` constructor slot (fieldName stays:
it maps to the ModelState key).

## Part B: nothing raw ever reaches the UI

- New `DatabaseAccessException(Exception)` in SmartERP.Data.Exceptions with a
  generic, non-revealing message ("The request could not be completed. Review
  the values you entered and try again.").
- Every repository's `catch (OracleException ...)` handler chain, after typed
  mapping, ends with `throw new DatabaseAccessException(ex)` — unmapped ORA
  codes can no longer escape.
- Controllers: the generic `catch (Exception)` blocks stop rendering
  `ex.Message`. They log the full exception and surface a shared generic string
  (`SmartERP.Web.UserMessages`). Typed exceptions keep their targeted copy.
- TOUCHES: 5 repositories, EmployeeController, DepartmentController,
  EmploymentController, DepartmentManagersController.

## Part C: confirm-and-replace current assignment

- `EmploymentDetailCreateViewModel` gains `ConfirmReplace` (bool).
- `IEmploymentDetailsRepository.CreateReplacingCurrentAsync(EmploymentDetail)`:
  single transaction -> close open row (`SET end_date = <new assignment start-date>
  WHERE employee_id = :Id AND end_date IS NULL`), then insert. Rollback on
  failure. Direct `CreateAsync` keeps its illegal-second-open guard.
- `EmploymentController.Create` GET: loads whether this employee has an open
  assignment (ViewData). When true the form gets an amber warning ("creating the
  new row will end your current assignment on <date>"), a checkbox
  "I understand the current assignment will end on <new start date>", and a
  `confirm()` gate on submit.
- POST: server checks existence of the open row again; replaces only when the
  checkbox says so, else returns the typed duplicate-current-job inline error.

## Verification

- Build clean; manual retest: replace-with-confirm creates history row + new open row;
  checkbox unticked -> inline typed error; any other exception path shows the
  generic string, never ORA text.

## Out of scope

- Department manager auto-replace (same UX requested only for employment records);
  decimal id refactor.
