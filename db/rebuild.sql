-- Rebuild the database from scratch. DATA-DESTRUCTIVE: drops every object,
-- recreates the current schema, loads the canonical seed. Use on a dev
-- instance you can reseed. For an existing database you want to keep, run
-- upgrade.sql instead (migrations only, non-destructive).
--
-- Run against the target instance with SQLcl/SQL*Plus:
--   sql usama@db @rebuild.sql

@./drop.sql
@./schema.sql
@./procedures.sql
@./triggers.sql
@./seed.sql
