# Plan: TASK-15 — app_users 1:1 with employees + audit_log via triggers

## Objective

Introduce application users (one per employee) with cookie-based authentication and role
claims, plus an audit trail written by Oracle triggers into `audit_log`. Authenticated users
only; destructive actions require the Admin role. The audit "who" is propagated from the app
to the database through `DBMS_SESSION.SET_IDENTIFIER` so triggers can record it.

## Dependencies

- None. TASK-01/TASK-12/TASK-14 closed.

## Files to modify

| Action | Path | What |
|--------|------|------|
| MODIFY | `db/00_drop.sql` | drop `audit_log`, `app_users` (and `roles` if used) |
| MODIFY | `db/01_schema.sql` | `app_users`, `audit_log` tables + FKs/checks/indexes |
| MODIFY | `db/03_triggers.sql` | audit triggers on the main tables |
| MODIFY | `db/04_seed.sql` | seed one admin app_user for a seeded employee; add counts |
| CREATE | `db/10_app_users_audit.sql` | guarded migration for populated databases |
| CREATE | `src/SmartERP.Data/Models/AppUser.cs` | id, employee id, username, password hash, role, is_active, timestamps |
| CREATE | `src/SmartERP.Data/Models/AuditEntry.cs` | id, table name, pk value, action, changed_by, changed_at |
| CREATE | `src/SmartERP.Data/Repositories/IAppUserRepository.cs` | get by username/id, create |
| CREATE | `src/SmartERP.Data/Repositories/AppUserRepository.cs` | parameterized SQL |
| CREATE | `src/SmartERP.Data/Abstractions/ICurrentUser.cs` | scoped user identity (username, role, employee id) |
| CREATE | `src/SmartERP.Data/Abstractions/IConnectionContext.cs` | AsyncLocal holder for the client identifier |
| MODIFY | `src/SmartERP.Data/Infrastructure/OracleDbConnection.cs` | set CLIENT_IDENTIFIER on open when present |
| MODIFY | `src/SmartERP.Web/Program.cs` | auth + DI wiring |
| CREATE | `src/SmartERP.Web/Controllers/AuthController.cs` | login/logout |
| CREATE | `src/SmartERP.Web/Models/LoginViewModel.cs` | username, password |
| CREATE | `src/SmartERP.Web/Views/Auth/Login.cshtml` | login form |
| CREATE | `src/SmartERP.Web/Services/PasswordService.cs` | PBKDF2 hash/verify |
| MODIFY | `src/SmartERP.Web/Controllers/*.cs` | `[Authorize]` (and `[Authorize(Roles="Admin")]` on destructive actions) |
| MODIFY | `src/SmartERP.Web/Views/Shared/_Layout.cshtml` | show user + logout link |
| MODIFY | `README.md`, `AGENTS.md`, `design.yaml` | docs |

## Implementation steps

1. **Schema.** `app_users(id, employee_id unique FK, username unique, password_hash, role check in ('Admin','HR','Viewer'), is_active default 1, timestamps)` and `audit_log(id, table_name, pk_value, action check in ('INSERT','UPDATE','DELETE'), changed_by, changed_at, index on table_name/pk_value and changed_at)`.

2. **Drop/migration.** Drop order: `audit_log` before `app_users` before `employees`. Guarded migration `db/10_app_users_audit.sql`.

3. **Triggers.** In `03_triggers.sql`, add one `AFTER INSERT OR UPDATE OR DELETE` trigger per main table (employees, departments, employment_details, department_managers, leave_requests, leave_balances, app_users) that inserts an `audit_log` row using `NVL(SYS_CONTEXT('USERENV','CLIENT_IDENTIFIER'), 'anonymous')` as `changed_by`, `:new.id`/`:old.id` as `pk_value`, and a literal table name.

4. **Connection context.** `IConnectionContext` (singleton) holds an `AsyncLocal<string?> ClientIdentifier`. `OracleDbConnection.CreateOpenConnectionAsync` executes `DBMS_SESSION.SET_IDENTIFIER` when the value is set. `ICurrentUser` (scoped) sets it at request start via middleware.

5. **Auth wiring.** `Program.cs`: `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => { o.LoginPath = "/Auth/Login"; })`, `AddAuthorization`, `UseAuthentication`/`UseAuthorization`. Register `ICurrentUser`, `IConnectionContext`, `IAppUserRepository`, `PasswordService`.

6. **Password service.** PBKDF2 via the static `Rfc2898DeriveBytes.Pbkdf2` one-shot method (instance constructors are obsolete): 100k iterations, SHA-256, 16-byte salt from `RandomNumberGenerator.GetBytes`, 32-byte hash. Stored as `base64(salt):base64(hash)`. Verify by re-deriving and comparing with `CryptographicOperations.FixedTimeEquals`. Do not use the obsolete instance constructors or their streaming `GetBytes`.

7. **AppUser repository.** `GetByUsernameAsync`, `GetByIdAsync`, `CreateAsync` (with typed exception mapping for duplicate username/employee).

8. **Auth controller.** Login GET/POST validates credentials, issues a cookie with claims (name, role, employee id, user id). Logout clears the cookie. On success, set `ICurrentUser` and the connection context identifier.

9. **Authorization.** Add `[Authorize]` to every controller. Add `[Authorize(Roles="Admin")]` to destructive actions (employee purge, department delete, balance upsert). Seed one admin user so the app remains usable.

10. **Middleware.** A small middleware sets `IConnectionContext.ClientIdentifier = user.Identity?.Name` for the request scope so triggers record the right "who".

11. **Seed.** In `04_seed.sql`, insert one admin app_user for a seeded employee with a documented default password hash. Add counts.

12. **Layout.** Show the signed-in user name and a logout link in the navbar.

13. **Docs.** README/AGENTS/design.yaml updates.

## Out of scope

- Password reset, MFA, account lockout, refresh tokens.
- Per-row data authorization (row-level security).
- Audit UI (reading audit_log in the app) — triggers write it; viewing is a later task.
- Migrating existing sessions (none exist).

## Definition of done

- [ ] Rebuild chain creates `app_users` and `audit_log`, their FKs/checks/indexes.
- [ ] Migration `10_app_users_audit.sql` is idempotent on a populated DB.
- [ ] Triggers fire on insert/update/delete of the main tables and write one audit row each with table name, pk, action, who, when.
- [ ] Unauthenticated requests redirect to `/Auth/Login`.
- [ ] Login with the seeded admin succeeds and sets the cookie; logout clears it.
- [ ] Destructive actions require the Admin role (non-admin gets 403).
- [ ] Audit rows record the signed-in username, not "anonymous", when an action is performed through the app.
- [ ] README, AGENTS.md and design.yaml describe the feature; build has no new warnings.
- [ ] Live Oracle smoke passes and the DB is restored.

## Verification criteria

- [ ] Every DoD checkbox evidenced.
- [ ] Lint, typecheck, tests, build all pass.

## Rationale

Cookie auth is the built-in, dependency-free option for ASP.NET Core MVC and matches the project's "no ORM, raw ADO.NET" stance. PBKDF2 keeps password hashing in-process without a new package. Propagating the user through `DBMS_SESSION.SET_IDENTIFIER` lets the Oracle triggers record "who" without changing every repository's SQL, and keeps audit honest for both app and direct-SQL writes. Role checks are limited to destructive actions to avoid breaking existing browse flows while still demonstrating authorization.