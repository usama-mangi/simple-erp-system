# Plan: TASK-12 — Option A.2: leave domain (leave_types, leave_requests, leave_balances)

User decisions (asked, answered):
1. `leave_requests.status` is a **lookup table** (`leave_statuses`), dropdown ids read from db.
2. `leave_balances` is a **stored balance** (`used_days` incremented in the same transaction as approval, decremented on cancelling an approved request).
3. Dashboard gains an **"On leave today" list + absence stats**.

## 1. Database

Four new tables, all following the existing conventions (identity PK, `created_at`/`updated_at`,
FK constraints with `fk_*` names, check constraints with `chk_*` names):

### `leave_types` (lookup)
- `id identity`, `title varchar2(30) not null` unique (`uq_leave_types_title`), timestamps.

### `leave_statuses` (lookup)
- `id identity`, `title varchar2(20) not null` unique (`uq_leave_statuses_title`), timestamps.
- Seeded **Pending, Approved, Rejected, Cancelled**. **Pending must land on id 1** (see index below):
  the seed inserts Pending as its own statement first, then merges the rest, then a guard block
  raises `raise_application_error(-20001, ...)` if `Pending`'s id is not 1.

### `leave_requests`
- `id identity`, `employee_id` FK (`fk_lr_employee`), `leave_type_id` FK (`fk_lr_leave_type`),
  `status_id` FK (`fk_lr_status` → leave_statuses), `start_date date not null`, `end_date date not null`,
  `reason varchar2(200)` nullable, `decided_at timestamp` (null while Pending), timestamps.
- `chk_lr_dates check (end_date >= start_date)` — `>=`, not `>`: a single-day leave is legal
  (unlike job assignments, where the old row must strictly end before the new one starts).
- **`uq_lr_open_request`**, function-based unique index, parallel to `uq_det_current_job`:
  ```sql
  create unique index uq_lr_open_request on leave_requests
    ( case when status_id = 1 then employee_id else null end );
  ```
  One Pending request per employee, enforced by the db. The literal `1` is the seeded id of
  `Pending`; the guard in the seed makes the invariant loud instead of silent. Documented in
  README + memory.

### `leave_balances`
- `id identity`, `employee_id` FK (`fk_bal_employee`), `leave_type_id` FK (`fk_bal_leave_type`),
  `year number(4)`, `entitlement_days number(5,1) not null`, `used_days number(5,1) default 0 not null`,
  timestamps.
- `uq_bal_employee_type_year unique (employee_id, leave_type_id, year)` — one row per employee/type/year.
- `chk_bal_year check (year between 2000 and 2100)`, `chk_bal_used check (used_days >= 0 and used_days <= entitlement_days)`.

### Script files (mirrors the TASK-11 pattern: fresh path + populated migration)
- `db/00_drop.sql`: drop `uq_lr_open_request`, then `leave_balances`, `leave_requests`,
  `leave_types`, `leave_statuses` — before `employees` drops.
- `db/01_schema.sql`: the four tables + `uq_lr_open_request`.
- `db/04_seed.sql`: new seed sections **before** the COMMIT block — `leave_statuses`
  (Pending first, own insert, then merge the rest, then id guard), `leave_types`
  (Annual, Sick, Personal, Unpaid), plus sample leave data so a fresh rebuild demos the domain:
  balance rows for a few employees for the current year and three sample requests
  (one Pending for an employee with no open request, one Approved covering `trunc(sysdate)`
  so "On leave today" renders, one Approved earlier in the current month so absence stats are
  non-zero). Merge keys are (employee email, leave_type title, start_date) so the seed stays re-runnable.
  Section numbers after the insertion shift (commit/verify become 11/12).
- **New `db/07_leave.sql`**: one-shot migration for populated installs, re-runnable like `06_positions.sql` —
  guarded `create table if not exists` ×4, status/type seeds, Pending-id guard, `drop index if exists
  uq_lr_open_request` + `create unique index`, verification selects. No sample request rows here
  (sysdate-relative dates would accumulate on re-run; the populated db gets its demo through the UI).
- `db/05_migrate.sql` unchanged (chain already runs 00..04, which now contain the leave objects).

## 2. Data layer (`SmartERP.Data`)

### Models (`Models/`)
- `LeaveRequest` (ids double, dates DateTime, status/reason/decided_at, joined names:
  employee name, type title, status title, computed `Days` = inclusive calendar days).
- `LeaveBalance` (with joined employee name, type title).
- `AbsenceStats` record (`DaysLostThisMonth`, `PendingRequests`, `OnLeaveToday` count).
- `Watchlists` record gains a 4th item `OnLeaveToday` (`IReadOnlyList<WatchlistEntry>`).

### Lookups
- `ILookupRepository` + impl: `GetLeaveTypesAsync()`, `GetLeaveStatusesAsync()` (db-driven ids,
  same `LookupItem` shape; never hard-code status/type ids in code — the controller resolves
  "Pending"/"Approved"/… by title from these lists).

### `ILeaveRepository` / `LeaveRepository` (leave aggregate, primary-constructor style,
`IDbConnectionFactory` + `OracleExceptionHandler`, async `Async` suffix, all SQL parameterized)
- `GetRequestsAsync(double? employeeId, double? statusId)` — newest first, joined titles.
- `GetRequestByIdAsync(double id)`.
- `GetOpenRequestAsync(double employeeId)` — `status_id = <pending id resolved from lookup>`.
- `CreateRequestAsync(LeaveRequest)` — ORA-00001 on `uq_lr_open_request` →
  `DuplicateOpenLeaveRequestException`; ORA-02290 on `chk_lr_dates` → `InvalidLeaveDatesException`.
- `ApproveAsync(LeaveRequest request, int year, double days)` — one transaction (spec's
  `await using`/async commit-rollback pattern): `UPDATE leave_requests SET status_id=:Approved,
  decided_at=systimestamp WHERE id=:Id AND status_id=:Pending` (0 rows → `NotFoundException`),
  then `UPDATE leave_balances SET used_days = used_days + :Days WHERE …` (0 rows → rollback +
  `InsufficientLeaveBalanceException`); ORA-02290 `chk_bal_used` maps to the same typed exception.
- `RejectAsync(double requestId)` — Pending → Rejected, single UPDATE, 0 rows → `NotFoundException`.
- `CancelAsync(LeaveRequest request)` — from Pending: status only; from Approved: status +
  `used_days = used_days - :Days` in one transaction (chk_bal_used as safety net).
- `GetBalancesAsync(double employeeId, int year)`.
- `UpsertBalanceAsync(LeaveBalance)` — merge on the unique key; entitlement update only (used_days
  never written here), lowering entitlement below used_days hits `chk_bal_used` → typed exception.

### Exceptions (`Exceptions/Constraint/Leave/`, mirroring `EmployeeDetail/`)
- `DuplicateOpenLeaveRequestException` (constraint name `uq_lr_open_request`).
- `InvalidLeaveDatesException` (`chk_lr_dates`).
- `InsufficientLeaveBalanceException` (`chk_bal_used` / missing balance row).

### Statistics (`IStatisticsRepository` + impl)
- `GetWatchlistsAsync`: add the 4th list — employees whose approved request range covers `trunc(sysdate)`
  (name, detail = type + end date), joined `employees.is_deleted = 0`.
- `GetAbsenceStatsAsync()`: days lost this month = Σ overlap of approved requests with the current
  calendar month (`least(end_date,last_day) - greatest(start_date,trunc(sysdate,'MM')) + 1`),
  pending count, on-leave-today count. sysdate-relative, so no new db script for the query itself.

### DI
- `Program.cs`: register `ILeaveRepository` → `LeaveRepository`.

## 3. Web layer (`SmartERP.Web`)

### `LeaveController` (mirrors `EmploymentController` patterns: primary ctor, typed-exception
catches → `ModelState`/`TempData`, `[ValidateAntiForgeryToken]` on every POST)
- `GET Index(double? employeeId, double? statusId)` — request list with filter dropdowns.
- `GET/POST Create(double employeeId)` — employee dropdown (soft-deleted excluded), leave-type
  dropdown, start/end/reason. App-level gates before insert:
  - `end_date >= start_date` (`IValidatableObject`);
  - overlap: any Pending/Approved request for that employee overlapping the range → inline
    `ModelState` error naming the conflicting dates;
  - open duplicate falls out of `uq_lr_open_request` → `DuplicateOpenLeaveRequestException`
    as an inline `ModelState` error.
- `POST Approve/Reject/Cancel` — request id + employee id for redirect; Approve resolves status ids
  from `GetLeaveStatusesAsync`, computes inclusive days and the `year` of `start_date`, and
  pre-checks the balance row: missing entitlement or `used + days > entitlement` → `TempData`
  error showing days remaining (db `chk_bal_used` stays as the safety net). Cancellation of an
  approved request decrements `used_days`.
- `GET Balances(double employeeId, int? year)` + `POST Upsert` — balance rows for the year with an
  add/edit entitlement form (defaults to current year).

### View models (`Models/`)
- `LeaveIndexViewModel` (requests + filter lists + employee name when filtered).
- `LeaveRequestCreateViewModel : IValidatableObject` (dates, reason, employee/type ids).
- `LeaveBalanceUpsertViewModel` (employee, type, year, entitlement).

### Views
- `Views/Leave/Index.cshtml` — filter row, status badges, date range, days, per-row actions
  (Approve/Reject on Pending; Cancel on Pending/Approved), link to Balances.
- `Views/Leave/Create.cshtml` — the form above.
- `Views/Leave/Balances.cshtml` — entitlement/used/remaining table + upsert form.
- `Views/Shared/_Layout.cshtml` — nav item **Leave** after Employees.
- `Views/Employee/Details.cshtml` — "Leave requests" link to `Leave/Index?employeeId=…`.

### Dashboard
- `Watchlists` 4th list rendered as an **"On leave today"** card in the watchlists section of
  `Views/Dashboard/Index.cshtml` (empty-state text when nobody is out).
- `AbsenceStats` on `DashboardIndexViewModel`: a small strip in the same section —
  days lost this month, pending requests awaiting decision, people out today.
- `DashboardController` loads both; a failure logs and marks the section (existing per-block
  try/catch style) without blanking the page.

## 4. Docs
- README: `db/` structure block gains `07_leave.sql`; data model gains the four tables with the
  open-request index and balance rules; relationships block gains
  `employees 1───* leave_requests *───1 leave_types`, `leave_requests *───1 leave_statuses`,
  `employees+leave_types 1───* leave_balances`; "What works" gains the leave bullet.

## 5. Verification
1. `dotnet build` clean (no new warnings from our files).
2. Live Oracle: run `db/07_leave.sql` against the populated db (guarded, re-runnable — run it twice,
   second run is a no-op), confirm verification selects and that Pending owns id 1.
3. Live smoke through the UI (`dotnet run`): create request → duplicate create for the same
   employee blocked with the typed message → overlap rejected inline → approve (balance row's
   `used_days` increments; verified by a direct query) → cancel an approved request decrements it →
   reject path → dashboard shows the on-leave-today card and non-zero absence numbers while a
   seeded/test approved request covers today → nav + employee Details links work.
4. Balance gates: approve beyond entitlement fails with the remaining-days message; `chk_bal_used`
   never fires from the app path (safety net only).
5. Restore db state to exactly prior after the smoke run (delete test-created requests/balances,
   re-verify row counts), same discipline as TASK-11.

## Out of scope
- Changing `employment_status` to 'On Leave' when a request is approved (leave domain stays
  independent of job assignments).
- Business-day/holiday counting (calendar days, inclusive, documented).
- ERD.png regeneration, decimal id refactor (TASK-05), tests.
