# Plan: TASK-17 — UI remediation: Employee + Department area onto the register system

## Objective

The Leave screens were built to `design.yaml`'s register-book direction (TASK-12/13/14). The
Employee and Department area only borrowed the shell: `register-surface`, `register-title`,
`register-panel`, `register-table`. Everything inside is still stock Bootstrap — blue/red/yellow
buttons, `form-group` fields, `table-dark` headers, `alert-success` with "Success!", no empty
states, no page titles on two indexes, and a stray flex wrapper that lays the Hierarchy page out
in a row. This task finishes the migration so the whole employee/department area speaks one
visual language, passes all four UI gates and all four intent gates, and records every screen in
`design.yaml`.

Mode: design-intent **REMEDIATE** (diagnose → re-decide → regenerate → gate). The register-book
direction, tokens and motion personality already exist and survive; nothing is rewritten from
scratch.

## Diagnosis (Phase R1, severity in brackets)

**Audience**
- "Success!"/"Error!", "Back to List", "Never Updated", generic Bootstrap voice on Details,
  Delete, both Update forms. Department/Create already says "Back to register" — two voices for
  the same action. [hurts-trust]
- `Employee/Index` and `Department/Index` set no `ViewData["Title"]` → empty `<title>`.
  [blocks-ship: delivery gate]
- `Department/Index` subtitle promises "who manages it" — the table has no manager column.
  [hurts-trust: name-swap/precision]

**Style**
- Raw Bootstrap components everywhere: `btn-primary/secondary/danger/warning`, `alert-success/
  warning`, `form-group/form-control`, `table-dark`, `card`. Their colours (#0d6efd, #dc3545,
  #ffc107, #212529) are not in the token set. [blocks-ship: system gate]
- Same form, two languages: Department/Create uses `.field` + `btn-register`; Employee/Create,
  Employee/Update, Department/Update, Employment/Create, DepartmentManagers/Create use raw
  `form-group`. [blocks-ship]
- `<thead class="table-dark">` (Department/Details, Department/Hierarchy): ink-500 text on
  #212529 computes to ~2.1:1. [blocks-ship: a11y AA]
- `Department/Hierarchy` wraps the entire page in `<div style="display:flex; flex-direction:row;
  justify-content:space-between">` — content lays out as a row. [blocks-ship]
- Inline `style="max-height: 420px; overflow-y: auto"` in 6 files, scroll tables with no sticky
  header (column labels scroll away). [hurts-trust]
- No empty state on Employee/Index, Department/Index, Hierarchy — blank grid on zero rows, and
  `register-empty` exists unused. [blocks-ship: HCI "no blank grid on no results"]
- `design.yaml screens:` registers none of the 9 Employee/Department screens. [blocks-ship:
  system gate cannot check what is not recorded]

**Motion**
- Details, Delete and all forms sit outside `.register-panel`, so they get no entrance; L2
  minimum is not met on those surfaces. The L3+ shared-header signature already exists and keeps
  working because every page keeps `.register-header`. [polish]

**Delivery**
- Missing: page titles (2), empty states (3), success/error parity with Leave (Decision-seal
  for success, token-styled warning for destructive), loading N/A (server-rendered, same
  exception as Leave).

**A11y (concrete violations)**
- Touch targets: `btn-warning/btn-danger/btn-secondary/btn-sm` render ~31–34px, under 44×44.
- Heading order: Details pages jump h1 → h3 → h5; Delete jumps h1 → h4.
- `Employee/Create`: `<label asp-for="IncludeEmploymentDetails">` emits `for=
  "IncludeEmploymentDetails"` but the input's id is `detailsToggle` → label does not toggle the
  field.
- `Department/Details` assign form: `<select name="ManagerId">` has no label at all (h5 is a
  heading, not a label).
- Links render Bootstrap #0d6efd (off-token).

## Decisions (Phase R2, named and recorded in design.yaml)

1. **Detail pages get a breadcrumb, not a back button.** A quiet `.register-back` text link
   above the title (44px target) replaces "Back to List"; the record action row then holds
   exactly two CTAs: primary `btn-register` + reject `btn-stamp--reject`. Counting rule recorded
   with the screens: `ctas` = the dominant action zone; breadcrumb links and section-local form
   submits are navigation.
2. **Record sheet.** `<dl>` on Details/Delete becomes a `RecordSheet` inside `register-panel`:
   uppercase micro-labels (ink-500) left, tabular values (ink-900) right, hairline row rules.
   Three real requests: Employee/Details, Department/Details, Employee/Delete (what is about to
   be removed) — clears the three-request rule.
3. **Row name is the link.** Employee/Department index rows link the person/department name to
   Details; the redundant Details button drops. Bigger target (Fitts), fewer buttons (Hick),
   delete stays the visually odd one out (Von Restorff).
4. **Sections use fieldset/legend for form groups**, headings for page sections (h1 → h2 only),
   fixing heading order without inventing heading levels.
5. **Feedback matches Leave**: success → `decision-seal` ("Recorded"); error → `alert-danger`
   (Leave precedent, kept); destructive warning → ochre-bordered notice reusing `status.pending`
   tokens (Employee/Delete, Employment/Create).
6. **"Current" open rows** use `stamp--current` on `feedback.success` (moss) with the word
   "Current", replacing `badge bg-success`.
7. **Copy is rewritten in register voice**: "Back to register", "Not updated yet", sentence-case
   titles, no "Success!"/"Error!", Department subtitle states only columns that exist.
8. **Scope boundary**: the two shared partials (`_EmploymentHistory`,
   `_DepartmentManagerHistory`) render inside the Details pages, so they convert too — which
   forces the four pages that render only those partials (Employment/Index, Employment/Create,
   DepartmentManagers/Index, DepartmentManagers/Create) to load the register system or show
   unstyled tables. 15 views total.

## Files to modify

| Action | Path | What |
|--------|------|------|
| MODIFY | `src/SmartERP.Web/wwwroot/css/register.css` | Add: `.register-back`, `.register-link`, `.record-sheet` (dl), `.register-panel--scroll` + sticky `thead th`, `.register-warn` notice, `.register-errors` validation summary, inline table controls ≥44px, `stamp--current`, scoped link colour + focus, section legend styling. Motion: panel entrance covers new zones; reduced-motion list updated. |
| MODIFY | `Views/Employee/Index.cshtml` | Title, subtitle fix, empty state + Create CTA, caption, name→Details link, actions Jobs/Update/Delete(reject), scroll class. |
| MODIFY | `Views/Employee/Details.cshtml` | Breadcrumb, h1 person name + meta (`RECORD #42 · UPDATED …`), h2 sections, RecordSheet, yyyy-mm-dd dates, decision-seal success, action row Update/Delete, end-of-record link to Leave register, panel wrapping. |
| MODIFY | `Views/Employee/Create.cshtml` | Title/subtitle/meta, register-panel, `.field` everywhere, fieldset/legend for employment details, fixed label/id pairing, `register-errors`, btn-register/btn-stamp. |
| MODIFY | `Views/Employee/Update.cshtml` | Same treatment as Create. |
| MODIFY | `Views/Employee/Delete.cshtml` | Breadcrumb, statement copy, register-warn for dependencies, RecordSheet of what is removed, native confirm on soft delete, checkbox-gated hard delete kept, 44px targets. |
| MODIFY | `Views/Department/Index.cshtml` | Title, truthful subtitle, empty state, caption, name→Details, scroll class. |
| MODIFY | `Views/Department/Details.cshtml` | Drop `table-dark`/`card`, breadcrumb, RecordSheet, h2 sections, labelled manager select, decision-seal, action row Update/Delete, drop redundant "Full Manage Page"/duplicate Back, register-link on parent/child links. |
| MODIFY | `Views/Department/Create.cshtml` | Minor: drop `col-md-4`, `register-errors`, "No parent (top level)". |
| MODIFY | `Views/Department/Update.cshtml` | Full `.field` treatment, register-panel, legend, buttons. |
| MODIFY | `Views/Department/Hierarchy.cshtml` | Remove stray flex wrapper, drop `table-dark`, breadcrumb, empty state, back/Create buttons to tokens. |
| MODIFY | `Views/Shared/_EmploymentHistory.cshtml` | `register-panel--scroll` + `register-table`, empty state instead of blank grid, `stamp--current`, yyyy-mm-dd, 44px close controls, Add job → btn-register, Transfer → btn-stamp. |
| MODIFY | `Views/Shared/_DepartmentManagerHistory.cshtml` | Same treatment. |
| MODIFY | `Views/Employment/Index.cshtml` | register-surface + header + Styles, decision-seal, title. |
| MODIFY | `Views/Employment/Create.cshtml` | Register form treatment + ochre warning notice. |
| MODIFY | `Views/DepartmentManagers/Index.cshtml` | register-surface + header + Styles, decision-seal, title. |
| MODIFY | `Views/DepartmentManagers/Create.cshtml` | Register form treatment. |
| MODIFY | `design.yaml` | Add 13 screens (dominant_action/message, ctas, components, scans), `RecordSheet` component with contrast pairs, `stamp.current` component tokens, `text.link` semantic token, log entry. |

## Implementation steps

1. `register.css`: tokens and treatments first (link, back, record-sheet, scroll+sticky, warn,
   errors, stamp--current, inline controls, legend). No view changes yet — CSS is inert until
   classes are used, so the build stays green.
2. Indexes: Employee/Index, Department/Index (titles, empty states, name links, captions).
3. Details: Employee/Details, Department/Details (breadcrumb, RecordSheet, headings, actions,
   feedback, labelled select).
4. Forms: Employee/Create, Employee/Update, Department/Update, Department/Create.
5. Delete: Employee/Delete (statement, warn, RecordSheet, confirms).
6. Hierarchy: remove flex wrapper, `table-dark`, empty state.
7. Partials + the four pages that render them.
8. `design.yaml`: screens, component, tokens, log.
9. Gate run: `python3 ~/.agents/skills/design-intent/scripts/intent.py scan`,
   `… intent.py gates`, then the four output gates by inspection against
   `references/slop-checklist.md` + `references/a11y-checklist.md`; build
   (`dotnet build`) and a click-through of every changed route.

## Out of scope

- Sorting, pagination and server-side search on the indexes: needs repository SQL; small
  internal register, Leave set the precedent without them. Follow-up if wanted.
- New data columns (department/position on Employee/Index): repository change, not a UI task.
- `Views/Auth`, `Dashboard`, `Leave/*` (already gated), `Employee/Details` status-history data.
- `_Layout` navbar restyle (global blast radius).

## Definition of done

- [ ] Zero Bootstrap component classes (`btn-primary/secondary/danger/warning`, `form-group`,
      `table-dark`, `card`, `alert-success`, `badge`) left in the 15 in-scope views.
- [ ] No inline `max-height`/`overflow` styles; no hex value added outside the token set.
- [ ] Every screen sets `ViewData["Title"]`; every list has an empty state with a Create path.
- [ ] Heading order h1 → h2 everywhere; every field labelled; every target ≥44×44; no text
      pair below 4.5:1 (the 2.1:1 `table-dark` header gone).
- [ ] Success feedback is `decision-seal` on all in-scope screens.
- [ ] `design.yaml` records all 13 screens + `RecordSheet`; `intent.py gates` exits 0;
      `intent.py scan` reports 0 slop findings.
- [ ] `dotnet build` passes; all changed routes render (rest, empty, error, success states).

## Verification criteria

- [ ] Every DoD checkbox evidenced by a grep or a rendered page.
- [ ] Build passes; four output gates + four intent gates green.

## Rationale

Remediate rather than rewrite: the register direction, tokens and motion already scored and
shipped, and the working markup (controllers, view models, POST flows) is untouched — this is
markup, copy and CSS only. Reusing `register.css` keeps one stylesheet as the system's single
source; adding search or new columns now would drag the data layer into a UI task and dilute the
gate run.
