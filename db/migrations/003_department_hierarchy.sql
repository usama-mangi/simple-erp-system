-- migrations/003_department_hierarchy.sql
-- One-shot migration for databases built by scripts 01..04 BEFORE departments
-- had a reporting line. A full rebuild (drop, schema, procedures,
-- triggers, seed) already produces the target shape, so every step
-- below is guarded and re-running this file is a no-op.
--
-- Steps:
--   1. add departments.parent_department_id (nullable: null = top level)
--   2. add fk_dept_parent, the self-referencing foreign key
--   3. add chk_dept_parent_not_self, the "a department cannot report to itself"
--      check
--   4. verify
--
-- What this file deliberately does NOT do:
--   - seed or invent parents. Existing rows keep parent_department_id = null,
--     so every department stays top level until someone sets a line in the UI.
--   - enforce acyclicity. Oracle can express "parent exists" and "not
--     yourself" declaratively but not "no loops"; the write path walks the
--     ancestor chain before saving.
--
-- Every statement is terminated by a lone "/" so runner tools and SQL*Plus
-- both execute the script as written, semicolon by semicolon.

-- =========================================================
-- 1. COLUMN
-- =========================================================

declare
   v_count number;
begin
   select count(*)
     into v_count
     from user_tab_cols
    where table_name = 'DEPARTMENTS'
      and column_name = 'PARENT_DEPARTMENT_ID';

   if v_count = 0 then
      execute immediate 'alter table departments add ( parent_department_id number )';
   end if;
end;
/

-- =========================================================
-- 2. FOREIGN KEY
--    No ON DELETE clause: deleting a department that still has
--    reports is blocked on purpose (ORA-02292).
-- =========================================================

declare
   v_count number;
begin
   select count(*)
     into v_count
     from user_constraints
    where table_name = 'DEPARTMENTS'
      and constraint_name = 'FK_DEPT_PARENT';

   if v_count = 0 then
      execute immediate 'alter table departments add constraint fk_dept_parent foreign key ( parent_department_id ) references departments ( id )';
   end if;
end;
/

-- =========================================================
-- 3. SELF-PARENT CHECK
-- =========================================================

declare
   v_count number;
begin
   select count(*)
     into v_count
     from user_constraints
    where table_name = 'DEPARTMENTS'
      and constraint_name = 'CHK_DEPT_PARENT_NOT_SELF';

   if v_count = 0 then
      execute immediate 'alter table departments add constraint chk_dept_parent_not_self check ( parent_department_id is null or parent_department_id != id )';
   end if;
end;
/

commit
/

-- =========================================================
-- 4. VERIFICATION
-- =========================================================

-- The column must exist. Healthy output is one row:
-- PARENT_DEPARTMENT_ID | NUMBER.
select column_name, data_type
  from user_tab_cols
 where table_name = 'DEPARTMENTS'
   and column_name = 'PARENT_DEPARTMENT_ID'
/

-- Both constraints must be enabled. Healthy output is two rows:
-- CHK_DEPT_PARENT_NOT_SELF | C | ENABLED
-- FK_DEPT_PARENT           | R | ENABLED
select constraint_name, constraint_type, status
  from user_constraints
 where table_name = 'DEPARTMENTS'
   and constraint_name in ( 'FK_DEPT_PARENT', 'CHK_DEPT_PARENT_NOT_SELF' )
 order by constraint_name
/

-- No row may report to itself. Healthy output is 0.
select count(*) self_parent_rows
  from departments
 where parent_department_id = id
/

-- Rows that already report somewhere, and rows still top level. On a fresh
-- migration the second number equals the department count.
select count(parent_department_id) with_parent,
       count(*) - count(parent_department_id) top_level
  from departments
/
