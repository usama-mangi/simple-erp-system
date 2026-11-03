-- migrations/007_review_cycles.sql
-- One-shot migration for databases built before the review cycle domain existed.
-- A full rebuild (drop, schema, procedures, triggers, seed) already produces the target shape,
-- so every step below is guarded and re-running this file is a no-op.

-- =========================================================
-- 1. TABLE
-- =========================================================

create table if not exists review_cycles (
   id          number generated always as identity,
   title       varchar2(100) not null,
   description varchar2(500),
   start_date  date not null,
   end_date    date not null,
   is_active   number(1) default 0 not null,
   created_at  timestamp default systimestamp not null,
   updated_at  timestamp,
   constraint pk_review_cycle_id primary key ( id ),
   constraint uq_review_cycles_title unique ( title ),
   constraint chk_review_dates check ( end_date >= start_date )
)
/

-- =========================================================
-- 2. SEED REVIEW CYCLES
-- =========================================================

merge into review_cycles rc
using (
   select '2025 Annual Review' title,
          'Annual performance review cycle for 2025' description,
          date '2025-01-01' start_date,
          date '2025-12-31' end_date,
          1 is_active
     from dual
   union all
   select '2026 Annual Review',
          'Annual performance review cycle for 2026',
          date '2026-01-01',
          date '2026-12-31',
          0
     from dual
   union all
   select 'Q1 2026 Review',
          'Quarterly review cycle for Q1 2026',
          date '2026-01-01',
          date '2026-03-31',
          0
     from dual
) s on ( rc.title = s.title )
when not matched then
insert (
   title,
   description,
   start_date,
   end_date,
   is_active )
values
   ( s.title,
     s.description,
     s.start_date,
     s.end_date,
     s.is_active )
/

commit;

-- =========================================================
-- 3. VERIFICATION
-- =========================================================

select table_name
  from user_tables
 where table_name = 'REVIEW_CYCLES'
/

select count(*) as cycle_count
  from review_cycles
/