-- migrations/012_applications.sql
-- One-shot migration for databases built before the applications domain existed.
-- Guarded and re-runnable.

-- =========================================================
-- 1. TABLE
-- =========================================================

create table if not exists applications (
   id                   number generated always as identity,
   candidate_id         number not null,
   requisition_id       number not null,
   stage_id             number not null default 1,
   status_id            number not null default 1,
   applied_date         date not null,
   last_updated_date    date,
   notes                varchar2(500),
   created_at           timestamp default systimestamp not null,
   updated_at           timestamp,
   constraint pk_application_id primary key ( id ),
   constraint fk_app_cand foreign key ( candidate_id )
      references candidates ( id ),
   constraint fk_app_req foreign key ( requisition_id )
      references requisitions ( id ),
   constraint fk_app_stage foreign key ( stage_id )
      references application_stages ( id ),
   constraint fk_app_status foreign key ( status_id )
      references application_stages ( id ),
   constraint chk_app_dates check ( applied_date <= nvl(last_updated_date, applied_date) )
)
/

-- =========================================================
-- 2. FUNCTION-BASED UNIQUE INDEX (Choice A)
--    one open application per candidate (where status_id = 1 = Started)
--    binds application.status_id to application_stages id 1, so the
--    invariant guard below fails loudly if Started is not id 1.
-- =========================================================

drop index if exists uq_cand_open_application
/

create unique index uq_cand_open_application on
   applications (
      case
         when
            status_id = 1
         then
               candidate_id
         else
            null
      end
   )
/

-- =========================================================
-- 3. INVARIANT GUARD
--    Fail loudly if the invariant is broken: Pending must be the first row
--    inserted into application_stages, so Started (id 1) must own id 1.
-- =========================================================

declare
   v_started_id number;
begin
   select id
     into v_started_id
     from application_stages
    where title = 'Started';

   if v_started_id != 1 then
      raise_application_error(
         -20001,
         'uq_cand_open_application binds application_stages id 1 to Started, but Started has id '
            || v_started_id
      );
   end if;
end;
/

commit;

-- =========================================================
-- 4. SEED APPLICATIONS (sample data)
-- =========================================================

merge into applications a
using (
   select 1 candidate_id, 1 requisition_id, 1 stage_id, 1 status_id, date '2026-01-15' applied_date, null last_updated_date, 'Initial application' notes from dual union all
   select 1, 1, 2, 1, date '2026-01-20', date '2026-01-22', 'Under review' from dual union all
   select 2, 2, 1, 1, date '2026-02-01', null, 'New application' from dual union all
   select 2, 2, 3, 2, date '2026-02-10', date '2026-02-12', 'Scheduled interview' from dual union all
   select 3, 1, 2, 1, date '2026-03-01', null, 'Re-applied' from dual
) s on ( a.candidate_id = s.candidate_id and a.requisition_id = s.requisition_id )
when not matched then
insert ( candidate_id, requisition_id, stage_id, status_id, applied_date, last_updated_date, notes )
values ( s.candidate_id, s.requisition_id, s.stage_id, s.status_id, s.applied_date, s.last_updated_date, s.notes )
/

commit

-- =========================================================
-- 5. VERIFICATION
-- =========================================================

select table_name from user_tables where table_name in ( 'APPLICATIONS' )
/

select a.id, c.first_name || ' ' || c.last_name as candidate, r.title as requisition,
       s1.title as stage, s2.title as status, a.applied_date
  from applications a
  join candidates c on c.id = a.candidate_id
  join requisitions r on r.id = a.requisition_id
  join application_stages s1 on s1.id = a.stage_id
  join application_stages s2 on s2.id = a.status_id
/

-- The unique index must exist and be usable.
select index_name, uniqueness
  from user_indexes
 where index_name = 'UQ_CAND_OPEN_APPLICATION'
/

-- Started must own id 1 (the index's binding). Healthy output is exactly
-- one row: 1 | Started.
select s.id, s.title
  from application_stages s
 where s.title = 'Started'
/
