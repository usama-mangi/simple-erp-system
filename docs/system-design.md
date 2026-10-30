# SmartERP system design

Date: 2026-10-30. Status: accepted for current scope.

## 1. Requirements

**Intent (falsifiable).** A single developer can manage HR master data through the MVC app with every Oracle constraint enforced end to end: by the time this is done, submitting a duplicate email, a negative salary, or a second open job hits a typed validation error in the UI, not an unhandled ORA exception.

**Users and flows.** One user (the developer), desktop browser. Critical flows:

1. List departments; create, update, delete one.
2. List employees; create, update, view one with current job details.
3. Record a job assignment (employment_details row) and close it with an end_date.
4. Assign a department manager and end that assignment.
5. Update lookup titles (genders, employment types, status) rarely, if ever. Read-only dropdowns in practice.

**Non-functional requirements.**

| Dimension | Decision |
|---|---|
| Scale | Single user, a few hundred seeded rows. No QPS pressure. |
| Latency | Sub-second page loads on LAN. Best effort, not measured. |
| Availability | Dev box only. No SLO. |
| Consistency | Read-your-writes on one connection per request. Trivially satisfied. |
| Durability | Oracle instance is the store; db/ scripts rebuild it. Loss tolerance is "reseed from scripts". |
| Data volume | Thousands of rows at most. No growth plan needed. |
| Read/write ratio | Roughly 1:1 during development; reads dominate once data exists. Doesn't drive design. |

**Constraints.** Solo developer. .NET 10 / C#. Oracle as the only database (chosen externally; non-negotiable). No cloud, no container. Budget: free. The learning goal is a constraint in itself: tools that hide SQL are excluded by intent.

**Security.** Learning project on localhost: no PII exposure beyond the home machine, but form posts still use antiforgery tokens and parameterized SQL as practice. No auth model yet, no encryption requirements, no audit trail beyond Oracle timestamps.

## 2. Capacity estimation

Numbers are absurd in this project's favor, recorded to show the level:

- QPS: 1 user x ~100 actions/day / 86400 = ~0.001 QPS average, ~0.01 peak. Any design passes.
- Storage: 1,000 employees x ~200 bytes + assignments ~400 bytes single-digit MB including indexes. Decades of headroom.
- Bandwidth: worst-case page ~50 KB HTML x 0.01 QPS = negligible.
- Latency budget: the only real cost is Oracle round-trips. At 1 ms per round-trip on localhost, a five-query request costs 5 ms. Query count per request is the efficiency lever, not indexing.

Conclusion: nothing here needs caching, replication, or denormalization. Design for clarity, not throughput.

## 3. Component decomposition

```
Browser (Razor views, form posts)
  -> SmartERP.Web
      Controllers (one per aggregate): EmployeeController, DepartmentController
      ViewModels per form (Create/Update/Delete shapes) with Validations/* attributes
  -> SmartERP.Data
      IEmployeeRepository / IDepartmentRepository                       (one per aggregate)
      EmploymentRepository, DepartmentManagerRepository, LookupRepository (to add)
      OracleExceptionHandler: OracleException -> typed Exceptions/*
      OracleDbConnection: IDbConnectionFactory over ODP.NET
  -> Oracle (db/01_schema.sql is the source of truth)
```

Single point of failure: the Oracle instance connection string. It is also the most-often-failing component (wrong credentials, listener down, schema not rebuilt). The failure path already funnels every open failure into the repository catch/throw; what is missing is a friendly render of that failure on the page instead of a 500.

Most-often-failing component in practice: human queries against schema drift. The `employee_details`/`e.department_id` mismatch in `EmployeeRepository.GetByIdAsync` is exactly this failure class. Rule going forward: schema names come from `db/01_schema.sql`, never from memory.

## 4. Data flow and contract

Hop by hop for a form post (department create):

1. Browser -> POST /Department/Create, form-encoded. Sync HTTP. Antiforgery token required.
2. ViewModel binding + attribute validation. Invalid -> re-render form with ModelState, HTTP 200.
3. Controller maps ViewModel -> domain model, calls `IDepartmentRepository`.
4. Repository opens its own connection per operation (no shared transaction scope across repositories), executes one parameterized statement.
5. OracleException path: `OracleExceptionHandler.Handle*` throws the typed exception (e.g. `DuplicateDepartmentCodeException` for ORA-00001 on uq_departments_code, `PrivelegeException` for insufficient rights). Controller catches, adds ModelState error, re-renders.
6. Success -> redirect to Index (PRG pattern).

Contract notes:

- Error semantics live in the exception names, not in Result wrappers. Each ORA code has exactly one mapped exception. The handler runs when the code is known and throws otherwise, rethrowing the original.
- IDs are `decimal` going forward (Oracle NUMBER mapping). `double` in read paths is legacy to be replaced during the Employee fix-up, not propagated.
- Idempotency: creates are not idempotent (unique constraints make retries fail loudly, which is desired). Updates overwrite the named columns.
- No versioning: views and controllers ship together.

## 5. Options considered

**Option A (chosen): repository-per-aggregate, raw ADO.NET, controller-as-orchestrator.** What it is: keep the existing shape. Each aggregate (employee, department, employment_details, department_managers, lookups) gets one interface and one implementation, hand-written SQL, manual mapping. Controllers orchestrate and map view models. No service layer, no ORM.

Pros: zero new concepts; the existing two repositories are the template; every ORA interaction is visible; smallest dependency graph.
Cons: mapping and connection plumbing duplicated per method; watch out for copy-paste schema drift (this already bit once).
When: solo devs learning the stack, which is this.

**Option B: Dapper + repository.** What it is: identical repository layout, Dapper replaces the reader/mapping choreography.

Pros: kills most row-mapping code; mapper-time column detection catches drift early.
Cons: extra dependency; change of shape for no felt pain among the existing mapping style; the courtship of OracleParameter types goes away, which is part of the learning.

**Option C: EF Core / EF Core with Oracle provider.** What it is: code-first entities, LINQ, migrations.

Pros: fastest to finish a feature; LINQ filtering and Includes for the employee-details join; constraint errors arrive translated, often poorly.
Cons: migrations fight the hand-maintained db/ scripts; ORA-code-specific exception typing requires custom interceptor work; the Oracle layer being built deliberately gets abstracted away.

**Trade-offs resolved.**

| Axis | Resolution | Why acceptable |
|---|---|---|
| Learning depth vs velocity | A loses velocity to ADO.NET boilerplate explicitly | Velocity is not the goal; correctness and understanding are |
| DRY vs explicitness | Every repository repeats open/execute/map plumbing | Duplication at this size beats another abstraction layer |
| Consistency failure mode | Chosen: loud errors (exception per constraint) over silent defaults | A wrong result that fails loudly is worth more than API polish here |
| Simplicity vs flexibility | Chosen: simplicity until a concrete driver exists | Sources: YAGNI; no scaling, sharing, or ML ambition exists for this project |

## 6. Validation and measurement

Assumptions stated as tests:

1. "OracleExceptionHandler covers every constraint this schema can raise." Verify by exercising each create/update with a bad payload and confirming the typed exception type, not just a non-500.
2. "Every repository method passes schema-check." Verify by running each method against the rebuilt schema once; the broken GetByIdAsync is proven broken exactly this way.
3. "The join-heavy employee read is a single query." Confirm query count by watching v$sql or trusting the single ExecuteReader call.

PoC scope: no spikes needed. All unknowns (Oracle transaction semantics for closing one job, opening another) are answered with a two-statement sequence against the real instance.

Measurement: skip load and fault injection. The only meaningful number is page latency on first load, for which no action is planned even if it is slow.

## 7. Decision and delivery

**Decision: Option A.** Trade-offs accepted: more boilerplate per repository method, slower feature velocity, and the standing risk of schema drift between SQL and C# strings.

Revisit triggers:
- Mapping plumbing grows to repeated multi-table joins with column-name mismatches twice more -> evaluate Dapper.
- A `db/` migration is ever needed that the scripts cannot express -> revisit EF migrations.
- A second consumer (API, scheduler, importer) needs the same data -> revisit a service layer above repositories.

Rollout and delivery: no canary story at one user. Scope order per backlog: finish Employee controller fix-up, then employment_details, then department_managers, lookup repositories as dependencies surface. No flags, no migration window: db scripts are the migration story.
