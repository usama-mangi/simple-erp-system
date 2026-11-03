# Plan: TASK-05 — Refactor ids from double to decimal across models, repositories, controllers and views

## Objective

Every database identity and foreign-key value flows through the app as `decimal` instead of
`double`. Oracle maps plain `NUMBER` to decimal without rounding drift; binary floating point
silently corrupts exercises like equality checks and balance math on ids. Measures (salary,
payroll, days, percents) are not ids and stay `double`. The sweep is mechanical but wide:
~219 `double` occurrences across ~60 files, of which roughly 30 are measures that must NOT
change. No behaviour is intended to change; verification is build plus live click-through.

## Dependencies

- TASK-01 (closed)

## Current state

- Identity columns in `db/01_schema.sql` are `number generated always as identity` (no scale);
  FK columns are `number`. ODP.NET returns these as `decimal` on read (existing code already
  switches on `decimal d =>` / `OracleDecimal`).
- C# models, repositories, view models, controllers and views declare the same values as
  `double`, reading them with `reader.GetDouble(...)` and binding parameters as
  `OracleDbType.Double`.
- ViewData titles are `Dictionary<double, string>` (5 views + 1 controller helper).
- `HttpContextCurrentUser` parses claims with `double.TryParse`.
- No automated tests exist; verification is build + live authenticated click-through.

## Target state

- Any id-typed property, parameter, dictionary key and local is `decimal`.
- Any measure-typed value (Salary, payroll/delta/percent, Entitlement/Used/Remaining days,
  dashboard `FormatNumber`, `[Range(...)]` bounds) stays `double`.
- Repository reads of id columns use `reader.GetDecimal(...)`; id parameters bind as
  `OracleDbType.Decimal`; id output parameters are `OracleDbType.Decimal`.
- Views render ids the same as before (scale-0 values render as "42", not "42.00"); hidden
  inputs round-trip cleanly through model binding.

## Files to modify (by layer; exhaustive via the rules below)

| Action | Path | What |
|--------|------|------|
| MODIFY | `src/SmartERP.Data/Models/*.cs` (19 files) | id properties double → decimal (Id, *Id, LeaveStatusIds members, parent/from/to status ids) |
| MODIFY | `src/SmartERP.Data/Repositories/*.cs` (15 files) | id parameters/locals/returns; `GetDouble` → `GetDecimal` on id columns only; `OracleDbType.Double` → `OracleDbType.Decimal` on id params |
| MODIFY | `src/SmartERP.Data/Repositories/EmploymentRepository.cs`, `src/SmartERP.Data/Repositories/EmployeeRepository.cs` | manual sites: `double? fromStatus` / `double? oldStatus` (status ids) incl. the `OracleDecimal` → decimal switch |
| MODIFY | `src/SmartERP.Data/Models/LeaveStatusIds.cs` | record members Pending/Approved/Rejected/Cancelled → decimal (they are status ids; do not match the suffix rule) |
| MODIFY | `src/SmartERP.Web/Models/*.cs` (all view models) | id properties → decimal; `[Range]` bounds stay double (attribute takes double; compiles unchanged) |
| MODIFY | `src/SmartERP.Web/Controllers/*.cs` (7 files) | action parameters, private helpers, `Dictionary<double, string>` → `Dictionary<decimal, string>`; `IdOf` returns decimal |
| MODIFY | `src/SmartERP.Web/Services/HttpContextCurrentUser.cs` | `double.TryParse` → `decimal.TryParse` (InvariantCulture) |
| MODIFY | `src/SmartERP.Web/Views/**` (6 views) | `Dictionary<double, string>` → `Dictionary<decimal, string>`; `Title(...)`/`StampFor(double statusId)` signatures |
| MODIFY | `AGENTS.md` | drop the now-false "Ids are double today" line; state ids are decimal |
| KEEP | `src/SmartERP.Data/Abstractions/EqualityChecker.cs` | unused user file; out of scope, untouched |
| KEEP | money/days doubles | Salary family, payroll/deltas, Entitlement/Used/Remaining days, dashboard FormatNumber — stay double |

Conversion rule (applied mechanically per file): change `double` → `decimal` (incl. `double?`)
only on tokens that are ids — standalone `Id`/`id`, identifiers ending in `Id`/`id`, or the
`IdOf` helper — plus the named exceptions above. `OracleDbType.Double` → `OracleDbType.Decimal`
only for id parameter names (`:Salary`, `:Days`, `:EntitlementDays`, `:E` stay `Double`).
`GetDouble` → `GetDecimal` only on id columns (`salary`, `total_salary`, `average_salary`,
`minimum_salary`, `maximum_salary`, `payroll`, `headcount`, `days_*`, `count`, `depth`,
`*_headcount` reads stay `GetDouble`).

## Implementation steps

1. **Data models.** Apply the conversion rule to `src/SmartERP.Data/Models/*.cs`; manually fix
   `LeaveStatusIds`. Verify: `grep -rn 'double' src/SmartERP.Data/Models` shows only measures.
2. **Data repositories.** Same rule; parameter types/bindings; `GetDecimal` on id columns;
   manual `oldStatus`/`fromStatus` sites and the `OracleDecimal` → decimal switch;
   `OracleDbType.Double` → `OracleDbType.Decimal` on id params (verify `:Salary`/`:Days`/
   `:EntitlementDays`/`:E` unchanged). Verify: no `GetDouble(...id...)` remains.
3. **Web layer.** View models, controllers (incl. `Dictionary<decimal, string>` and `IdOf`),
   `HttpContextCurrentUser` (invariant `TryParse`), views (dictionary types, helper params).
   Verify: `grep -rn '\bdouble\b' src | grep -v /obj/` lists only the KEEP set above.
4. **Build fix loop.** `dotnet build`; resolve every fallout until 0 errors. Any site that
   cannot compile without changing a measure is a rule misapplication — fix the site, not the
   measure.
5. **Smoke constants check.** Confirm no literal `0`/`0.0` needed `m` suffix anywhere it bound
   against decimal (implicit conversion covers `0`; build catches the rest).
6. **Runtime verification.** Rebuild, restart app, login as seeded admin, click through all
   changed routes (Employee/Department Index/Details/Create/Update/Delete, Employment
   Index/Create, DepartmentManagers Index/Create, Hierarchy, Dashboard) and POST one
   representative round trip (Employee/Update save). Confirm: HTTP 200s, decision-seal renders,
   hidden `Id` inputs round-trip, `RECORD #` prints without trailing zeros, no error pages.
7. **AGENTS.md** one-line truth fix re ids being decimal now.
8. Run full gate: `dotnet build` + the click-through checklist; route to audit in review mode.

## Out of scope

- Money and day-count columns (Salary, payroll, entitlement/used/remaining days): a separate
  decision is needed on whether money goes decimal too; not mixed into an id refactor.
- `EqualityChecker.cs` (unused, user's file), `FormatNumber`, `[Range]` attribute bounds.
- Any ORM or query rewrite: binding stays ODP.NET with explicit `OracleDbType`.
- New tests (none exist in the project yet).

## Definition of done

- [ ] Zero `double` (incl. `double?`) remains on id-typed members, parameters, dictionary keys
      or locals; grep evidence committed in the review output.
- [ ] `OracleDbType.Decimal` binds every id parameter; `GetDecimal` reads every id column;
      salary/day bindings unchanged.
- [ ] Build passes with 0 errors and no new warnings beyond the pre-existing NU1903/NU1902 and
      SonarQube target warnings.
- [ ] Live click-through of all listed routes returns 200 with authenticated session; one POST
      round trip succeeds; inspected markup shows clean integer ids.
- [ ] AGENTS.md line updated.

## Verification criteria

- [ ] Every DoD checkbox evidenced by grep output, build log or HTTP result.
- [ ] Build passes; audit (review mode) pass recommendation.

## Rationale

Suffix-based targeting keeps the ~30 measure doubles untouched without per-file judgment
calls, and the compiler plus a deliberate audit pass catch anything the suffix rule misses
(e.g. the two `*Status` locals). An id refactor and a money refactor would each be reviewable
alone; mixed, neither is. `OracleDbType.Decimal` and `GetDecimal` are native ODP.NET mappings
for NUMBER, so binding semantics actually improve (no silent binary rounding in comparisons).
