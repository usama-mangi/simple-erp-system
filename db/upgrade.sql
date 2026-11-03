-- Upgrade an existing database in place. Runs every migration in order.
-- Migrations are guarded and re-runnable, so re-running this file is a
-- no-op once applied. No drops, no data reload.
--
-- Run against the target instance with SQLcl/SQL*Plus:
--   sql usama@db @upgrade.sql

@./migrations/001_positions.sql
@./migrations/002_leave.sql
@./migrations/003_department_hierarchy.sql
@./migrations/004_status_events.sql
@./migrations/005_app_users_audit.sql
@./migrations/006_documents.sql
