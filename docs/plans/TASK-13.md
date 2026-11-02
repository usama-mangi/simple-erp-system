# Plan: TASK-13 — departments.parent_department_id org hierarchy

## Objective

Give departments a reporting line: a nullable `parent_department_id` self-FK, surfaced through
department create/update, a reporting-structure page that reads the tree depth-first, and a
rolled-up open headcount per department (own staff plus everyone below it). The schema keeps the
hierarchy honest (FK, no self-parent); the app refuses cycles and shows the tree without a
denormalised cache that could drift.

## Dependencies

- None. TASK-01/TASK-12 are closed and the department read/write path exists.

## Current state

- `departments` has id, name, code, location, established, timestamps. No parent column.
- `DepartmentRepository` reads and writes those five business columns only; `Department` carries
  no parent.
- Department pages: `Index` (flat table), `Details` (header + manager assignment), `Create`,
  `Update`. No hierarchy read anywhere.
- Headcount exists only inside `StatisticsRepository` (month-end trend, payroll by department),
  never per department node.

## Files to modify

| Action | Path | What |
|--------|------|------|
| MODIFY | `db/01_schema.sql` | `departments.parent_department_id number`, `fk_dept_parent`, `chk_dept_parent_not_self` |
| CREATE | `db/08_department_hierarchy.sql` | Guarded, re-runnable migration for populated databases + verification selects |
| MODIFY | `src/SmartERP.Data/Models/Department.cs` | `ParentDepartmentId`, `ParentName` |
| CREATE | `src/SmartERP.Data/Models/DepartmentNode.cs` | Flat tree row: parent, depth, direct headcount, rollup headcount |
| CREATE | `src/SmartERP.Data/Exceptions/Constraint/Department/DepartmentParentSelfException.cs` | Maps `CHK_DEPT_PARENT_NOT_SELF` to ModelState key `ParentDepartmentId` |
| MODIFY | `src/SmartERP.Data/Repositories/IDepartmentRepository.cs` | Hierarchy reads, parent candidates, cycle check |
| MODIFY | `src/SmartERP.Data/Repositories/DepartmentRepository.cs` | Parent in selects/insert/update; recursive hierarchy query; cycle + candidate queries |
| MODIFY | `src/SmartERP.Web/Models/DepartmentCreateViewModel.cs` | `ParentDepartmentId?`, `ParentOptions` |
| MODIFY | `src/SmartERP.Web/Models/DepartmentUpdateViewModel.cs` | Same |
| MODIFY | `src/SmartERP.Web/Models/DepartmentDetailsViewModel.cs` | `Children`, `DirectHeadcount`, `RollupHeadcount` |
| CREATE | `src/SmartERP.Web/Models/DepartmentHierarchyViewModel.cs` | Ordered nodes for the reporting page |
| MODIFY | `src/SmartERP.Web/Controllers/DepartmentController.cs` | Parent validation, cycle guard, hierarchy action, details rollup |
| MODIFY | `src/SmartERP.Web/Views/Department/Create.cshtml` | "Reports to" select + help text |
| MODIFY | `src/SmartERP.Web/Views/Department/Update.cshtml` | Same, options exclude self and descendants |
| MODIFY | `src/SmartERP.Web/Views/Department/Index.cshtml` | "Reports to" column + link to reporting structure |
| MODIFY | `src/SmartERP.Web/Views/Department/Details.cshtml` | Parent link, child list, direct vs rolled-up headcount |
| CREATE | `src/SmartERP.Web/Views/Department/Hierarchy.cshtml` | Depth-first tree table + empty state |
| MODIFY | `src/SmartERP.Web/wwwroot/css/site.css` | `.dept-tree-indent` depth helper (token multiple) |
| MODIFY | `README.md` | Script list, departments column, relationship, What works |
| MODIFY | `AGENTS.md` | Departments bullet + migration script list |
| MODIFY | `design.yaml` | `screens.DepartmentHierarchy`, log entry |

## Implementation steps

1. **Schema (rebuild path).** In `01_schema.sql` add to `departments`:
   `parent_department_id number`, `constraint fk_dept_parent foreign key (parent_department_id) references departments (id)`,
   `constraint chk_dept_parent_not_self check (parent_department_id is null or parent_department_id <> id)`.
   Verify by running the rebuild chain in filename order on the live instance.

2. **Migration (populated path).** `db/08_department_hierarchy.sql`, in the shape of `07_leave.sql`:
   guard the column add with `user_tab_cols`, the FK and check with `user_constraints`, then commit.
   End with verification selects: column present, `FK_DEPT_PARENT` enabled, `CHK_DEPT_PARENT_NOT_SELF`
   enabled, null parent counts per department. Run the file twice; the second run must be a no-op.

3. **Model + typed exception.** Add `double? ParentDepartmentId` and `string? ParentName` to
   `Department`. `DepartmentNode` = `Id, Name, Code, ParentDepartmentId, ParentName, Depth,
   DirectHeadcount, RollupHeadcount`. `DepartmentParentSelfException` follows
   `DuplicateDepartmentNameException` (constraint `CHK_DEPT_PARENT_NOT_SELF`, field
   `ParentDepartmentId`, message "A department cannot report to itself.").

4. **Repository contract.** `IDepartmentRepository` gains:
   - `Task<IEnumerable<DepartmentNode>> GetHierarchyAsync()`
   - `Task<IEnumerable<Department>> GetParentCandidatesAsync(double? excludeDepartmentId)`
   - `Task<bool> WouldCreateCycleAsync(double departmentId, double parentDepartmentId)`

5. **Repository implementation.**
   - `GetAllAsync`/`GetByIdAsync`: select `parent_department_id` + `LEFT JOIN departments p` for
     `parent_name`; map both.
   - `CreateAsync`/`UpdateAsync`: bind `:ParentDepartmentId` as `double` with
     `(object?)value ?? DBNull.Value`; register `DepartmentParentSelfException` alongside the two
     duplicate constraints in both catch blocks.
   - `GetHierarchyAsync`: one statement, three CTEs — `dept_tree` (roots → children via `UNION ALL`,
     carrying `depth` and a name-based `sort_path`), `subtree` (every department × every descendant),
     `rollup` (`COUNT(DISTINCT employee_id)` of open `employment_details` joined to non-deleted
     employees). Select direct headcount per node with a correlated count of the same shape, left
     join rollup, `ORDER BY sort_path`.
   - `GetParentCandidatesAsync`: recursive `descendants` CTE seeded with the excluded id, select every
     department not in it; `NULL` exclusion returns all.
   - `WouldCreateCycleAsync`: `SELECT COUNT(*)` over a `START WITH id = :ParentId CONNECT BY NOCYCLE
     PRIOR parent_department_id = id` walking ancestors, filtered to `:DepartmentId`.

6. **View models.** Create/Update get `double? ParentDepartmentId` (no validation attribute — empty
   means top level) and `IEnumerable<SelectListItem> ParentOptions` defaulted to empty. Details gets
   `IEnumerable<DepartmentNode> Children`, `int DirectHeadcount`, `int RollupHeadcount`.
   New `DepartmentHierarchyViewModel` holds `IEnumerable<DepartmentNode> Nodes`.

7. **Controller.**
   - `PopulateParentOptions(excludeId)` private helper: `GetParentCandidatesAsync`, ordered by name,
     `SelectListItem` values = id strings.
   - `Create` GET: populate options. `Create` POST: on invalid model repopulate and return the view;
     pass `ParentDepartmentId` through to `CreateAsync`; catch typed exceptions into `ModelState`
     (`ConstraintException.FieldName`) or the message, matching the department create pattern.
   - `Update` GET: populate options excluding the department itself and its descendants.
     `Update` POST: existence check, self check (`ParentDepartmentId == Id`), then
     `WouldCreateCycleAsync`; any hit is a `ModelState` error on `ParentDepartmentId` and returns the
     view with options repopulated. Otherwise `UpdateAsync` with the parent.
   - `Details`: fetch hierarchy once; find this department's node for the two counts and collect nodes
     whose `ParentDepartmentId` equals this id as `Children`.
   - New `[HttpGet] Hierarchy()`: `GetHierarchyAsync`, wrap in the view model; failure logs and sets
     `TempData["ErrorMessage"]` then redirects to `Index` (existing pattern).

8. **Views.**
   - Create/Update: `Reports to` select with `<option value="">No parent — top level</option>` and
     `asp-items="Model.ParentOptions"`, help text "Leave blank for a top-level department."
   - Index: `Reports to` column renders `ParentName` or `Top level`; header gains a
     `Reporting structure` link beside `Create Department`.
   - Details: `Reports to` (link to parent details or "Top-level department"), `Open headcount`,
     `Including sub-departments`, and a `Departments reporting here` table (code, name, direct
     headcount, link) with "No department reports here." empty state.
   - Hierarchy: heading `Reporting structure`, one-line standfirst naming the register, table with
     Department (indented by depth), Code, Reports to, Open headcount, Including sub-departments.
     Empty state: "No departments are set up yet." plus the create link. Indentation comes from
     `.dept-tree-indent` fed a `--tree-depth` custom property; the Reports to column carries the
     relationship in text, so nothing depends on indentation alone.
   - Copy stays in the `design.yaml` voice: plainspoken, exact, no "org chart platform" language.

9. **Docs + design system.** README: add `08_department_hierarchy.sql` to the script tree, note
   `parent_department_id` in the departments row, add the self-relationship, extend the Departments
   What-works bullet. AGENTS.md: departments bullet names the nullable self-FK and the new script.
   `design.yaml`: add `DepartmentHierarchy` under `screens` (dominant action read, dominant message
   "Every department, what reports into it, and the open headcount under it", components
   `RegisterPanel`), refresh `log.last_updated`.

10. **Gate.** `dotnet build SmartERP.slnx`; run `08` twice; run the app against the live Oracle and
    exercise: create a temporary child department, view the tree + rollups, reparent it, try
    self-parent and a two-level cycle, open its details, delete a parent that still has a child
    (must fail cleanly), then delete the temporary rows and confirm every parent is back to `NULL`.

## Out of scope

- Dashboard rollups per department, chart changes, drag-and-drop chart editing.
- Cascading department deletes or reparenting history (FK blocks delete; that is the contract).
- The missing department delete view/GET action (TASK-06) and the double → decimal id refactor (TASK-05).
- A trigger or model-level acyclic guarantee beyond the app check — accepted single-writer risk.

## Definition of done

- [ ] Rebuild chain creates `parent_department_id`, `FK_DEPT_PARENT`, `CHK_DEPT_PARENT_NOT_SELF`;
      `08_department_hierarchy.sql` produces the same state on a populated DB and is a no-op on a
      second run.
- [ ] Department create and update persist a parent; blank clears it; the update dropdown never
      offers the department or its descendants.
- [ ] Self-parent and ancestor-as-parent attempts are rejected with a `ParentDepartmentId` ModelState
      error; the DB check constraint also rejects a self-parent with the typed exception.
- [ ] Details shows the parent, direct children and both headcount figures; the hierarchy page lists
      every department depth-first with the same figures.
- [ ] Deleting a department that has children still fails through the existing
      `ChildFoundException` path, with no rows lost.
- [ ] README, AGENTS.md and design.yaml describe the feature; UI gates (knowledge, intent, slop,
      a11y, system) pass; build has no new warnings.
- [ ] Live Oracle smoke passes and the database is restored to its pre-smoke state.

## Verification criteria

- [ ] Every DoD checkbox evidenced (SQL output, HTTP responses, build log).
- [ ] Lint · Typecheck · Tests · Build all pass (no test project exists; live smoke is the test).

## Rationale

A self-FK plus an application-level ancestor walk is the smallest correct model: Oracle can express
"parent exists" and "not yourself" declaratively but not "no cycles", so the write path checks the
chain with `CONNECT BY NOCYCLE`. Headcount rollups are computed at read time from the same
`employment_details` rows the rest of the app trusts, instead of adding a cached column that would
drift every time a job opens or closes. The hierarchy page reuses the flat node list the details page
already needs, so one new query serves both screens.
