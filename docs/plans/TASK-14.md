# Plan: TASK-14 — status_events table for status churn history

## Objective

Add an append-only `status_events` table that records every employment-status transition per
employee (from, to, when, note), so status churn and exits can be read as a real time series
instead of inferred from the final status stamped on a closed assignment row. The current
`employment_details.employment_status_id` stays the authoritative final-status read, so every
existing read (current status, exits-by-status, watchlists) keeps working unchanged.

## Dependencies

- None. TASK-01/TASK-02/TASK-12 closed; employment and employee write paths exist.

## Files to modify

| Action | Path | What |
|--------|------|------|
| MODIFY | `db/00_drop.sql` | drop `status_events` before `employment_details` |
| MODIFY | `db/01_schema.sql` | `status_events` table + FKs/check + index |
| MODIFY | `db/04_seed.sql` | backfill one event per seeded assignment; add count to verification block |
| CREATE | `db/09_status_events.sql` | guarded, re-runnable migration for populated databases + verification |
| CREATE | `src/SmartERP.Data/Models/StatusEvent.cs` | id, employee id, from/to status ids + titles, changed_at, note |
| MODIFY | `src/SmartERP.Data/Repositories/IEmploymentRepository.cs` | `GetStatusEventsAsync(double employeeId)` |
| CREATE | `src/SmartERP.Data/Repositories/StatusEventWriter.cs` | shared insert (binds one SQL string in both repos) |
| MODIFY | `src/SmartERP.Data/Repositories/EmploymentRepository.cs` | insert events on create/replace; read events |
| MODIFY | `src/SmartERP.Data/Repositories/EmployeeRepository.cs` | insert events on create-with-details and on status change; purge events on destructive delete |
| MODIFY | `src/SmartERP.Web/Models/EmployeeDetailsViewModel.cs` | `StatusEvents` list |
| MODIFY | `src/SmartERP.Web/Controllers/EmployeeController.cs` | fetch events on Details |
| MODIFY | `src/SmartERP.Web/Views/Employee/Details.cshtml` | "Status history" table + empty state |
| MODIFY | `src/SmartERP.Data/Repositories/StatisticsRepository.cs` | watchlist `days_in_status` reads latest event with fallback |
| MODIFY | `README.md` | script list, table list, migration note |
| MODIFY | `AGENTS.md` | status_events bullet and script list |
| MODIFY | `design.yaml` | log entry |

## Implementation steps

1. **Schema (rebuild path).** In `01_schema.sql`, after `employment_details`, add
   `status_events(id, employee_id, from_status_id nullable, to_status_id not null, changed_at
   timestamp default systimestamp, note varchar2(200))` with FKs to `employees` and
   `employment_status` (twice), and `chk_se_transition (from_status_id is null or
   from_status_id != to_status_id)`. Add `ix_se_employee_at (employee_id, changed_at)`.

2. **Drop order.** Add `drop table if exists status_events;` to `00_drop.sql` ahead of
   `employment_details`.

3. **Backfill (seed).** In `04_seed.sql`, after the employment_details merge, insert one event
   per assignment: `from_status_id = LAG(employment_status_id) OVER (PARTITION BY employee_id
   ORDER BY start_date, id)`, `to_status_id = employment_status_id`, `changed_at = start_date`,
   `note = 'Backfilled from assignment history'`, guarded by `NOT EXISTS (any event for that
   employee)`. Add a `Status Events` line to the verification counts.

4. **Migration (populated path).** `db/09_status_events.sql`: guarded create table/FKs/check/index
   (each step guarded via `user_tab_cols`/`user_constraints`/`user_indexes`), the same backfill
   with a per-employee `NOT EXISTS` guard, and verification selects (table present, both FKs and
   the check enabled, index present, event count, no row with from = to). Runnable twice as a no-op.

5. **Model.** `StatusEvent` with `double Id`, `double EmployeeId`, `double? FromStatusId`,
   `string? FromStatusTitle`, `double ToStatusId`, `string ToStatusTitle`, `DateTime ChangedAt`,
   `string? Note`.

6. **Repository contract.** `IEmploymentRepository` gains `GetStatusEventsAsync(double employeeId)`.

7. **Shared writer.** `StatusEventWriter` (internal static) holds one `InsertSql` string and an
   `InsertAsync` that binds `:EmployeeId`, `:FromStatusId` (DBNull when null), `:ToStatusId`,
   `:ChangedAt`, `:Note` and runs on the supplied connection/transaction.

8. **EmploymentRepository.**
   - `GetStatusEventsAsync`: join `employment_status` twice for titles, filter by employee, order
     `changed_at DESC, id DESC`.
   - `CreateAsync`: wrap the insert in a transaction; after the detail insert, write an event
     `null -> status` at `detail.StartDate` with note "New assignment started".
   - `CreateReplacingCurrentAsync`: change the close `UPDATE` to use `RETURNING
     employment_status_id INTO :FromStatus` so the previous status is captured without a second
     read; after the new-row insert, write an event `from -> new status` at `detail.StartDate`
     with note "Replaced the previous assignment". Keep the transaction.
   - `GetStatusEventsAsync` ordering as above.

9. **EmployeeRepository.**
   - `CreateWithDetailsAsync`: after the detail insert, write an event `null -> status` at
     `employee.StartDate` with note "First assignment recorded", in the same transaction.
   - `UpdateAsync`: fix the pre-existing history bug by adding `AND end_date IS NULL` to the detail
     `UPDATE`. Before it, lock and read the open row's status with
     `SELECT employment_status_id ... FOR UPDATE`; if a row exists and its status differs from the
     new one, write an event `old -> new` at `systimestamp` with note "Status changed on the
     employee record", in the same transaction.
   - `DeleteWithDetailsAsync`: add `DELETE FROM status_events WHERE employee_id = :Id` before
     `employment_details` in the purge transaction.

10. **Web model/controller/view.** Add `IReadOnlyList<StatusEvent> StatusEvents` to
    `EmployeeDetailsViewModel`; fetch it in `EmployeeController.Details`; render a "Status history"
    table (When / From / To / Note) below the employment history partial, with an empty state
    "No status changes recorded yet." Dates formatted `yyyy-MM-dd`, blank note rendered as an em
    dash. No new component, no new token.

11. **Statistics.** In `WatchlistQuery` and `PendingExitsQuery`, replace
    `TRUNC(SYSDATE) - ed.start_date` with the latest event's `changed_at` when one exists,
    falling back to `ed.start_date` (so legacy rows keep their current value). Add a
    `LEFT JOIN (SELECT employee_id, MAX(changed_at) AS status_since FROM status_events GROUP BY
    employee_id)`.

12. **Docs + design system.** README: add `09_status_events.sql` to the script tree, add the table
    to the table list, drop the "no history table" sentence and replace it with a status_events
    description. AGENTS.md: add `status_events` to the twelve-tables... thirteen count, and note
    the migration script. `design.yaml`: log entry noting the status history subsection.

13. **Gate.** `dotnet build SmartERP.slnx`; run `09` twice; run the app and exercise create (with
    and without details), replace, update status, delete-with-details, and the status history view;
    verify watchlist days-in-status. Restore the DB to its pre-smoke state.

## Out of scope

- Recording an event when an assignment is closed (exit date is already on the row; status is
  unchanged on close).
- Manual note entry from the UI.
- A status-churn chart on the dashboard.
- The double-to-decimal id refactor (TASK-05) and the missing department delete view (TASK-06).

## Definition of done

- [ ] Rebuild chain creates `status_events`, its FKs, check and index; `09_status_events.sql`
      produces the same state on a populated DB and is a no-op on a second run.
- [ ] Seeded employees have one backfilled event per assignment; the count matches
      `employment_details`.
- [ ] Create, create-with-details, replace and status-update all write exactly one event each;
      unchanged status updates write none.
- [ ] Purging an employee deletes their events before the employee row; soft delete keeps them.
- [ ] Employee details renders the status history with correct dates and titles, plus an empty state.
- [ ] Watchlist `days_in_status` uses the latest event when present and falls back to start date.
- [ ] `UpdateAsync` only touches the open row (history rows no longer rewritten).
- [ ] README, AGENTS.md and design.yaml describe the feature; UI gates pass; build has no new warnings.
- [ ] Live Oracle smoke passes and the database is restored.

## Verification criteria

- [ ] Every DoD checkbox evidenced.
- [ ] Lint, typecheck, tests, build all pass (no test project; live smoke is the test).

## Rationale

A separate history table is the smallest way to keep the final status readable on the row while
still knowing when a status was entered. Writes are recorded in the same transaction as the
status change, so an event can never drift from the row it describes; the shared writer keeps the
SQL in one place across the two repositories. Using `RETURNING` on the close update captures
the previous status without a second round trip, and locking the open row in `UpdateAsync` with
`SELECT ... FOR UPDATE` avoids a stale read under concurrent edits. The watchlist fallback to
`start_date` means rows created before the migration keep their current behaviour until the
backfill runs. Fixing the missing `end_date IS NULL` predicate in `UpdateAsync` while we are
there prevents the new event logic from firing against closed rows and stops historical rows
from being rewritten.