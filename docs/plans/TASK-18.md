# Plan: TASK-18 — Documents: metadata vault + expiry watchlist

## Objective

Add the first slice of the Option B domains: a metadata-only document vault per employee,
surfaced on a per-employee Documents page (list/add/edit/remove), with an "expires within
30 days" watchlist card on the HR dashboard. No file storage, no scanning, no DMS features.
Ships with the seed unification the user decided: `04_seed.sql` becomes the single seed
(the old `11_realistic_seed.sql` is folded in), and new seed data for the vault tables
lives there too.

## Dependencies

- None (split from TASK-16; TASK-19 reviews follow this)

## Current state

- Schema: 15 tables (incl. leave + app_users/audit_log). Employee destructive purge and
  `HasDependenciesAsync` know nothing about documents.
- `Watchlists` record on `IStatisticsRepository.GetWatchlistsAsync` feeds the dashboard
  watchlist cards (probation, pending exits, unmanaged departments); each is one query.
- Register system (register.css) covers tables, empty states, buttons; dashboard itself
  is Card-based Bootstrap and out of register scope, left as-is.
- Rebuild is `00_drop` → `01_schema` → … → `11_realistic_seed`; README documents this.
  `11_realistic_seed.sql` (250 employees, uncommitted until now, committed in f08b0a9)
  duplicates the lookups of `04_seed.sql` and seeds a different app user (`admin`) than
  the working one (`ali.khan`) — merging must preserve both.

## Target state

- Employees keep named documents (contract, passport, certifications…) as metadata rows.
- A Documents page per employee lists them with Type / Issued / Expires / Days left /
  Status (VALID / DUE SOON / EXPIRED as stamped words), add/edit/delete inline.
- Dashboard gains a fourth watchlist card: documents expiring within 30 days, open to the
  Documents page.
- One seed script builds the whole schema + data; README/AGENTS.md match reality.

## Decisions (agreed with user 2026-11-03)

1. Metadata only — no BLOB/file storage, no download/upload.
2. Only manager-subordinate reviews (TASK-19); documents have no such restriction.
3. Seed unification happens in this task: `04_seed.sql` becomes the single seed.
4. `12_documents.sql` is the guarded migration for populated installs; `01_schema.sql`
   is canonical. New tables also go in `01_schema.sql`.

## Files to modify / create

| Action | Path | What |
|--------|------|------|
| MODIFY | `db/01_schema.sql` | add `document_types` and `documents` (after leave tables) + `ix_documents_expires`, `ix_documents_employee` |
| MODIFY | `db/00_drop.sql` | drop `documents` and `document_types` (children before parents) |
| MODIFY | `db/03_triggers.sql` | add `trg_audit_documents`, `trg_audit_document_types` (audit_log pattern) |
| CREATE | `db/12_documents.sql` | guarded, re-runnable: create tables/indexes if not exists + triggers create or replace |
| REWRITE | `db/04_seed.sql` | merged single seed: realistic bulk seed + lookup merges + `ali.khan` app_user merge + `document_types` + ~a dozen `documents` with staggered expiries (some past, some within 30d, some far) + final count summary |
| DELETE | `db/11_realistic_seed.sql` | folded into 04_seed.sql |
| MODIFY | `db/12_documents.sql` | (created) — see above |
| CREATE | `src/SmartERP.Data/Models/Document.cs`, `DocumentType.cs` | `Document(Id, EmployeeId, TypeId, TypeTitle?, Title, IssuedOn, ExpiresAt, Note, CreatedAt)`; `DocumentType(Id, Title)` |
| MODIFY | `src/SmartERP.Data/Models/Watchlists.cs` | add `IReadOnlyList<ExpiringDocument> ExpiringDocuments`; new record `ExpiringDocument(EmployeeName, DocumentTitle, TypeTitle, ExpiresAt, DaysLeft)` |
| CREATE | `src/SmartERP.Data/Models/ExpiringDocument.cs` | (if not inline in Watchlists.cs) |
| CREATE | `src/SmartERP.Data/Repositories/IDocumentRepository.cs`, `DocumentRepository.cs` | `GetForEmployeeAsync(employeeId)`, `GetExpiringAsync(days)`, `GetByIdAsync(id)`, `CreateAsync(doc)`, `UpdateAsync(doc)`, `DeleteAsync(id)`, `HasDependenciesAsync(employeeId)`-style existence for purge? (not needed — documents are deleted wholesale in purge) |
| MODIFY | `src/SmartERP.Data/Repositories/ILookupRepository.cs`, `LookupRepository.cs` | add `GetDocumentTypesAsync()` (dropdown source) |
| MODIFY | `src/SmartERP.Data/Repositories/StatisticsRepository.cs`, `IStatisticsRepository.cs` | `GetWatchlistsAsync` gains expiring-documents query (join documents + document_types + employees/employment_details + departments; `expires_at IS NOT NULL AND expires_at <= TRUNC(SYSDATE) + :Days AND is_deleted = 0`; map to `ExpiringDocument`) |
| MODIFY | `src/SmartERP.Data/Repositories/EmployeeRepository.cs` | destructive purge deletes `documents` rows for the purged employee before `employees` (after leave rows, before employee delete); `HasDependenciesAsync` counts documents too |
| CREATE | `src/SmartERP.Web/Models/DocumentCreateViewModel.cs`, `DocumentUpdateViewModel.cs` (or one `DocumentFormViewModel` + `DocumentsIndexViewModel`) | validation, `[Required]` title/type, date ordering check via `IValidatableObject` (expires ≥ issued when both set) |
| CREATE | `src/SmartERP.Web/Controllers/DocumentsController.cs` | Index(employeeId), Create GET/POST, Edit GET/POST, Delete POST (static confirm text); `[Authorize]`; Admin-only not required (metadata, not destructive business data — delete is inline POST + confirm) |
| CREATE | `src/SmartERP.Web/Views/Documents/Index.cshtml`, `Create.cshtml`, `Edit.cshtml` | register system: `register-panel--scroll` + `register-table`, empty state with Add CTA, stamps `stamp--current` (VALID) / `stamp--pending` (DUE SOON) / `stamp--rejected` (EXPIRED), `decision-seal` on POST success, 44px targets, captions, ViewData["Title"] everywhere, breadcrumb to employee |
| MODIFY | `src/SmartERP.Web/Views/Employee/Details.cshtml` | end-of-record link "Documents for this employee" (mirrors the Leave link) |
| MODIFY | `src/SmartERP.Web/Views/Employee/Delete.cshtml` | copy: documents purged too; checkbox text updated |
| MODIFY | `src/SmartERP.Web/Views/Dashboard/Index.cshtml`, `src/SmartERP.Web/Models/DashboardIndexViewModel.cs` (+ controller) | fourth watchlist card "Documents expiring within 30 days" (Employee / Document / Expires / Days) linking to the Documents page; count==0 line |
| MODIFY | `README.md` | db layout (12_documents.sql), feature list, seed story (single 04_seed.sql) |
| MODIFY | `AGENTS.md` | table count 15 → 17 (`document_types`, `documents`), seed layout note, purge now includes documents |
| MODIFY | `design.yaml` | screens `DocumentsIndex`, `DocumentsCreate`, `DocumentsEdit` (components RegisterPanel + LedgerField; ctas 1–2; slop/hci pass), log entry; bump `status` |

## Implementation steps

1. **Schema + dropped-set.** `01_schema.sql`, `00_drop.sql`, `03_triggers.sql`,
   `12_documents.sql` (idempotent by construction: `if not exists` / `create or replace`).
   Verify with a full rebuild dry run? No — rebuild is the user's call to run against
   their live instance; verification here = syntax-review the scripts + `sql` lint by
   inspection (no runner available in this environment).
2. **Seed.** Compose merged `04_seed.sql` (realistic seed verbatim + ali.khan merge block +
   `document_types` merge + `documents` bulk block + count summary incl. new tables);
   delete `11_realistic_seed.sql`; README/AGENTS updated in the same step.
3. **Data layer.** Models → `IDocumentRepository`/`DocumentRepository` (GetForEmployee,
   GetExpiring, CRUD; map join to `TypeTitle`) → `LookupRepository.GetDocumentTypesAsync` →
   `StatisticsRepository` expiring-docs query → `EmployeeRepository` purge +
   `HasDependenciesAsync` extension. Build green.
4. **Web layer.** ViewModels (+ `IValidatableObject` date ordering) → `DocumentsController`
   → views → Employee/Details link → Employee/Delete copy → Dashboard card + VM. Build green.
5. **Runtime verification.** Rebuild; authenticate; click through:
   Documents index (blocked? no employee without docs required — empty state renders),
   create doc on a *throwaway employee* created via the UI (mark it "documents test"),
   edit it, see it in the dashboard watchlist if expiry ≤ 30d, delete it, then run the
   employee **delete-with-dependencies purge** on the throwaway (validates the extended
   purge + `HasDependenciesAsync`), confirm the employee and its documents are gone.
   Confirm `/Documents?employeeId=<deleted>` is not reachable with stale id (redirects or
   empty state with note).
6. `design.yaml` screens + log; `intent.py scan`, `intent.py gates`; full build; audit
   (review mode); record-progress; commit `feat(TASK-18)`.

## Out of scope

- File storage / upload / download / malware scanning (metadata only by decision).
- Per-document ACLs or document-level audit beyond the table's audit trigger.
- Expiry notifications/email (dashboard watchlist is the reminder).
- Editing `document_types` from the UI (lookup seed is enough).
- Deleting a document via a confirmation page (inline POST + static confirm; metadata-only).
- Events/jobs to auto-expire documents (they expire by date, no state).

## Definition of done

- [ ] Schema: fresh rebuild chain includes document tables (reviewed scripts); 00_drop
      drops them; `12_documents.sql` re-runnable (`if not exists` / `create or replace`).
- [ ] Single seed: `db/` has exactly one seed script; it creates `ali.khan` **and** the
      realistic set and document fixtures; README/AGENTS match.
- [ ] `Documents/Index?employeeId=` renders list + stamps + empty state; Create/Edit POST
      round trips work (INVALID input rejected with ModelState, valid input → seal →
      Index); Delete removes the row (native confirm, static string).
- [ ] Dashboard watchlist shows documents expiring within 30 days (fixture rows seeded
      relative to SYSDATE prove this); count==0 line renders when none.
- [ ] Employee purge removes the throwaway employee's documents; `HasDependenciesAsync`
      returns true when documents exist (warning + checkbox path unchanged but texts
      updated).
- [ ] No new Bootstrap component classes in changed views; `intent.py gates` exits 0;
      `intent.py scan` CLEAN; `dotnet build` 0 errors.

## Verification criteria

- [ ] Every DoD checkbox evidenced by HTTP result, rendered markup, or script inspection.
- [ ] Build passes; audit (review mode) pass recommendation.

## Rationale

Documents is the smallest of the three Option B domains and the one with no workflow, so
it de-risks the rest. A per-employee page (not a global registry) keeps the authority
where HR looks — next to the employee's jobs and leave — and the dashboard watchlist is
the one new reporting affordance, reusing the existing watchlist pattern rather than
inventing a second report. Seed unification lands here because this task is already
touching every db script; doing it later would mean editing the same files twice.
Deleting via inline confirm (not a confirmation page) is deliberate: document metadata is
cheap to recreate and the employee-delete pattern exists for the irreversible case.
