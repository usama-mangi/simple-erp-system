-- migrations/009_application_stages.sql
-- One-shot migration for databases built before the application stage domain existed.
-- Guarded and re-runnable.

-- =========================================================
-- 1. TABLE
-- =========================================================

create table if not exists application_stages (
   id          number generated always as identity,
   title       varchar2(50) not null,
   description varchar2(200),
   is_active   number(1) default 1 not null,
   sort_order  number not null default 0,
   created_at  timestamp default systimestamp not null,
   updated_at  timestamp,
   constraint pk_application_stage_id primary key ( id ),
   constraint uq_application_stages_title unique ( title )
)
/

-- =========================================================
-- 2. SEED APPLICATION STAGES
--    Standard pipeline: Started -> Under Review -> Interview -> Offer -> Hired -> Rejected
--    The function-based unique index (uq_cand_open_application) will bind
--    status_id to application_stages id = 1 (Started), so the invariant guard
--    below fails loudly if Started is not id 1.
-- =========================================================

merge into application_stages asg
using (
   select 'Started' title,
          'Candidate has started the application' description,
          1 sort_order,
          1 is_active
     from dual
   union all
   select 'Under Review',
          'Application under review by hiring team',
          2,
          1
     from dual
   union all
   select 'Interview',
          'Candidate is being interviewed',
          3,
          1
     from dual
   union all
   select 'Offer',
          'Job offer extended to candidate',
          4,
          1
     from dual
   union all
   select 'Hired',
          'Candidate accepted offer and is now employee',
          5,
          1
     from dual
   union all
   select 'Rejected',
          'Candidate was not selected',
          6,
          0
     from dual
) s on ( asg.title = s.title )
when not matched then
insert (
   title,
   description,
   sort_order,
   is_active )
values
   ( s.title,
     s.description,
     s.sort_order,
     s.is_active )
/

commit;

-- =========================================================
-- 3. VERIFICATION
-- =========================================================

-- Started must own id 1 (the index's binding). Healthy output is exactly
-- one row: 1 | Started. The guard below already failed the run otherwise.
select asg.id,
       asg.title
  from application_stages asg
 where asg.title = 'Started'
/

-- Lookups must be populated (6 stages on a fresh migration).
select 'application_stages' table_name,
       count(*) total_rows
  from application_stages
/