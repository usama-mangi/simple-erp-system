-- 07_leave.sql
-- One-shot migration for databases built by scripts 01..04 BEFORE the leave
-- domain existed. A full rebuild (00_drop, 01_schema, 02_procedures,
-- 03_triggers, 04_seed) already produces the target shape, so every step
-- below is guarded and re-running this file is a no-op.
--
-- Steps:
--   1. create leave_types, leave_statuses, leave_requests, leave_balances
--   2. seed leave_statuses (Pending first) and guard that Pending owns id 1
--   3. seed leave_types
--   4. updated_at triggers for the two real-data tables
--   5. uq_lr_open_request, the one-open-request-per-employee index
--   6. verify
--
-- No sample request rows here: 04_seed's samples are sysdate-relative, and
-- re-running a migration against a populated database would stack a fresh
-- "on leave today" row on every run. The populated database gets its demo
-- data through the UI instead.
--
-- Every statement is terminated by a lone "/" so runner tools and SQL*Plus
-- both execute the script as written, semicolon by semicolon.

-- =========================================================
-- 1. TABLES
-- =========================================================

create table if not exists leave_types (
   id         number generated always as identity,
   title      varchar2(30) not null,
   created_at timestamp default systimestamp not null,
   updated_at timestamp,
   constraint pk_leave_type_id primary key ( id ),
   constraint uq_leave_types_title unique ( title )
)
/

create table if not exists leave_statuses (
   id         number generated always as identity,
   title      varchar2(20) not null,
   created_at timestamp default systimestamp not null,
   updated_at timestamp,
   constraint pk_leave_status_id primary key ( id ),
   constraint uq_leave_statuses_title unique ( title )
)
/

create table if not exists leave_requests (
   id            number generated always as identity,
   employee_id   number not null,
   leave_type_id number not null,
   status_id     number not null,
   start_date    date not null,
   end_date      date not null,
   reason        varchar2(200),
   decided_at    timestamp,
   created_at    timestamp default systimestamp not null,
   updated_at    timestamp,
   constraint pk_leave_request_id primary key ( id ),
   constraint fk_lr_employee foreign key ( employee_id )
      references employees ( id ),
   constraint fk_lr_leave_type foreign key ( leave_type_id )
      references leave_types ( id ),
   constraint fk_lr_status foreign key ( status_id )
      references leave_statuses ( id ),
   constraint chk_lr_dates check ( end_date >= start_date ) -- a single-day leave is legal
)
/

create table if not exists leave_balances (
   id               number generated always as identity,
   employee_id      number not null,
   leave_type_id    number not null,
   year             number(4) not null,
   entitlement_days number(5,1) not null,
   used_days        number(5,1) default 0 not null,
   created_at       timestamp default systimestamp not null,
   updated_at       timestamp,
   constraint pk_leave_balance_id primary key ( id ),
   constraint fk_bal_employee foreign key ( employee_id )
      references employees ( id ),
   constraint fk_bal_leave_type foreign key ( leave_type_id )
      references leave_types ( id ),
   constraint uq_bal_employee_type_year unique ( employee_id, leave_type_id, year ),
   constraint chk_bal_year check ( year between 2000 and 2100 ),
   constraint chk_bal_used check ( used_days >= 0 and used_days <= entitlement_days )
)
/
commit
/

-- =========================================================
-- 2. SEED LEAVE STATUSES
--    Pending gets its own statement first: uq_lr_open_request
--    binds leave_statuses id 1 to Pending, so Pending must be
--    the first row ever inserted into the table. The guard
--    below fails loudly if the invariant is broken.
-- =========================================================

insert into leave_statuses ( title )
select 'Pending'
  from dual
 where not exists (
       select 1
         from leave_statuses
        where title = 'Pending'
   )
/

merge into leave_statuses ls
using (
   select 'Approved' title
     from dual
   union all
   select 'Rejected'
     from dual
   union all
   select 'Cancelled'
     from dual
) s on ( ls.title = s.title )
when not matched then
insert ( title )
values
   ( s.title );
/
commit
/

declare
   pending_id number;
begin
   select id
     into pending_id
     from leave_statuses
    where title = 'Pending';

   if pending_id != 1 then
      raise_application_error(
         -20001,
         'uq_lr_open_request binds leave_statuses id 1 to Pending, but Pending has id '
            || pending_id
      );
   end if;
end;
/

-- =========================================================
-- 3. SEED LEAVE TYPES
-- =========================================================

merge into leave_types lt
using (
   select 'Annual' title
     from dual
   union all
   select 'Sick'
     from dual
   union all
   select 'Personal'
     from dual
   union all
   select 'Unpaid'
     from dual
) s on ( lt.title = s.title )
when not matched then
insert ( title )
values
   ( s.title );
/
commit
/

-- =========================================================
-- 4. TRIGGERS
--    create or replace is idempotent, so a re-run only
--    refreshes the definition.
-- =========================================================

create or replace trigger trg_set_lr_updated_at before
   update on leave_requests
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/

create or replace trigger trg_set_bal_updated_at before
   update on leave_balances
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/

-- =========================================================
-- 5. OPEN-REQUEST INDEX
--    Drop-then-create keeps the file re-runnable; creating it
--    over rows that already violate one-open-per-employee
--    would be a real error and should fail the migration.
-- =========================================================

drop index if exists uq_lr_open_request
/

create unique index uq_lr_open_request on
   leave_requests (
      case
         when
            status_id = 1
         then
               employee_id
         else
            null
      end
   )
/

-- =========================================================
-- 6. VERIFICATION
-- =========================================================

-- Pending must own id 1 (the index's binding). Healthy output is exactly
-- one row: 1 | Pending. The guard above already failed the run otherwise.
select ls.id, ls.title
  from leave_statuses ls
 where ls.title = 'Pending'
/

-- Lookups must be populated (4 statuses, 4 types on a fresh migration).
select 'leave_statuses' table_name,
       count(*) total_rows
  from leave_statuses
union all
select 'leave_types',
       count(*)
  from leave_types
union all
select 'leave_requests',
       count(*)
  from leave_requests
union all
select 'leave_balances',
       count(*)
  from leave_balances
/

-- The index must exist and be usable. Healthy output is one row:
-- UQ_LR_OPEN_REQUEST | UNIQUE.
select index_name, uniqueness
  from user_indexes
 where index_name = 'UQ_LR_OPEN_REQUEST'
/

-- Constraints must be tracked. Healthy output lists the named ones below
-- plus the NOT NULL system checks: FK_LR_EMPLOYEE, FK_LR_LEAVE_TYPE,
-- FK_LR_STATUS, CHK_LR_DATES, FK_BAL_EMPLOYEE, FK_BAL_LEAVE_TYPE,
-- UQ_BAL_EMPLOYEE_TYPE_YEAR, CHK_BAL_YEAR, CHK_BAL_USED.
select uc.constraint_name, uc.constraint_type, uc.status
  from user_constraints uc
 where uc.table_name in ( 'LEAVE_REQUESTS', 'LEAVE_BALANCES' )
   and uc.constraint_type in ( 'R', 'C', 'U' )
 order by uc.table_name, uc.constraint_name
/
