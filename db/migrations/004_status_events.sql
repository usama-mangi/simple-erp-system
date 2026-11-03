-- migrations/004_status_events.sql
-- One-shot migration for databases built by scripts 01..04 BEFORE status churn
-- history existed. A full rebuild (drop, schema, procedures, triggers,
-- seed) already produces the target shape, so every step below is guarded and
-- re-running this file is a no-op.
--
-- Steps:
--   1. create status_events
--   2. add its FKs and the transition check
--   3. add ix_se_employee_at
--   4. backfill one event per existing assignment
--   5. verify
--
-- Every statement is terminated by a lone "/" so runner tools and SQL*Plus
-- both execute the script as written, semicolon by semicolon.

-- =========================================================
-- 1. TABLE
-- =========================================================

create table if not exists status_events (
   id              number generated always as identity,
   employee_id     number not null,
   from_status_id  number,
   to_status_id    number not null,
   changed_at      timestamp default systimestamp not null,
   note            varchar2(200),
   constraint pk_status_event_id primary key ( id )
)
/

-- =========================================================
-- 2. FOREIGN KEYS AND CHECK
-- =========================================================

declare
   v_count number;
begin
   select count(*)
     into v_count
     from user_constraints
    where table_name = 'STATUS_EVENTS'
      and constraint_name = 'FK_SE_EMPLOYEE';
   if v_count = 0 then
      execute immediate 'alter table status_events add constraint fk_se_employee foreign key ( employee_id ) references employees ( id )';
   end if;
end;
/

declare
   v_count number;
begin
   select count(*)
     into v_count
     from user_constraints
    where table_name = 'STATUS_EVENTS'
      and constraint_name = 'FK_SE_FROM_STATUS';
   if v_count = 0 then
      execute immediate 'alter table status_events add constraint fk_se_from_status foreign key ( from_status_id ) references employment_status ( id )';
   end if;
end;
/

declare
   v_count number;
begin
   select count(*)
     into v_count
     from user_constraints
    where table_name = 'STATUS_EVENTS'
      and constraint_name = 'FK_SE_TO_STATUS';
   if v_count = 0 then
      execute immediate 'alter table status_events add constraint fk_se_to_status foreign key ( to_status_id ) references employment_status ( id )';
   end if;
end;
/

declare
   v_count number;
begin
   select count(*)
     into v_count
     from user_constraints
    where table_name = 'STATUS_EVENTS'
      and constraint_name = 'CHK_SE_TRANSITION';
   if v_count = 0 then
      execute immediate 'alter table status_events add constraint chk_se_transition check ( from_status_id is null or from_status_id != to_status_id )';
   end if;
end;
/

-- =========================================================
-- 3. INDEX
-- =========================================================

declare
   v_count number;
begin
   select count(*)
     into v_count
     from user_indexes
    where index_name = 'IX_SE_EMPLOYEE_AT';
   if v_count = 0 then
      execute immediate 'create index ix_se_employee_at on status_events ( employee_id, changed_at )';
   end if;
end;
/

-- =========================================================
-- 4. BACKFILL
-- =========================================================

insert into status_events (employee_id, from_status_id, to_status_id, changed_at, note)
select ed.employee_id,
       lag(ed.employment_status_id) over (
          partition by ed.employee_id
          order by ed.start_date, ed.id ),
       ed.employment_status_id,
       cast(ed.start_date as timestamp),
       'Backfilled from assignment history'
  from employment_details ed
 where not exists (
          select 1
            from status_events se
           where se.employee_id = ed.employee_id );

commit
/

-- =========================================================
-- 5. VERIFICATION
-- =========================================================

select table_name
  from user_tables
 where table_name = 'STATUS_EVENTS'
/

select constraint_name, constraint_type, status
  from user_constraints
 where table_name = 'STATUS_EVENTS'
 order by constraint_name
/

select index_name
  from user_indexes
 where index_name = 'IX_SE_EMPLOYEE_AT'
/

select count(*) as status_event_rows from status_events
/

select count(*) as bad_rows
  from status_events
 where from_status_id is not null
   and from_status_id = to_status_id
/