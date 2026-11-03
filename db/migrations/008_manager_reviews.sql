-- migrations/008_manager_reviews.sql
-- One-shot migration for databases built before the manager review domain existed.
-- A full rebuild (drop, schema, procedures, triggers, seed) already produces the target shape,
-- so every step below is guarded and re-running this file is a no-op.

-- =========================================================
-- 1. TABLE
-- =========================================================

create table if not exists manager_reviews (
   id               number generated always as identity,
   employee_id      number not null,
   reviewer_id      number not null,
   review_cycle_id  number not null,
   rating           number(1,1) not null,
   comments         varchar2(500),
   review_date      date default sysdate not null,
   created_at       timestamp default systimestamp not null,
   updated_at       timestamp,
   constraint pk_manager_review_id primary key ( id ),
   constraint fk_mr_employee foreign key ( employee_id ) references employees ( id ),
   constraint fk_mr_reviewer foreign key ( reviewer_id ) references employees ( id ),
   constraint fk_mr_review_cycle foreign key ( review_cycle_id ) references review_cycles ( id ),
   constraint chk_review_rating check ( rating >= 1 and rating <= 5 ),
   constraint uq_mr_employee_cycle_reviewer unique ( employee_id, review_cycle_id, reviewer_id )
)
/

-- =========================================================
-- 2. SEED MANAGER REVIEWS (sample data for existing employees)
-- =========================================================

declare
   v_2025_cycle number;
   v_q1_cycle number;
begin
   -- Get the 2025 annual review cycle id
   select id into v_2025_cycle from review_cycles where title = '2025 Annual Review';

   -- Get the Q1 2026 review cycle id
   select id into v_q1_cycle from review_cycles where title = 'Q1 2026 Review';

   -- Sample reviews for a few employees (only if cycles exist)
   if v_2025_cycle is not null then
      merge into manager_reviews mr
      using (
         select 101 as employee_id, 102 as reviewer_id, v_2025_cycle as review_cycle_id, 4.5 as rating, 'Excellent performance in 2025' comments, date '2025-12-15' review_date from dual union all
         select 103, 104, v_2025_cycle, 3.8, 'Good overall, needs improvement in time management' comments, date '2025-12-15' from dual union all
         select 105, 106, v_2025_cycle, 2.2, 'Below expectations, performance concerns documented' comments, date '2025-12-15' from dual
      ) s on ( mr.employee_id = s.employee_id and mr.review_cycle_id = s.review_cycle_id and mr.reviewer_id = s.reviewer_id )
      when not matched then
      insert ( employee_id, reviewer_id, review_cycle_id, rating, comments, review_date )
      values ( s.employee_id, s.reviewer_id, s.review_cycle_id, s.rating, s.comments, s.review_date );
   end if;

   if v_q1_cycle is not null then
      merge into manager_reviews mr
      using (
         select 101 as employee_id, 107 as reviewer_id, v_q1_cycle as review_cycle_id, 4.8 as rating, 'Strong start to 2026' comments, date '2026-03-15' review_date from dual union all
         select 103, 108, v_q1_cycle, 4.2, 'Good progress on Q1 goals' comments, date '2026-03-15' from dual
      ) s on ( mr.employee_id = s.employee_id and mr.review_cycle_id = s.review_cycle_id and mr.reviewer_id = s.reviewer_id )
      when not matched then
      insert ( employee_id, reviewer_id, review_cycle_id, rating, comments, review_date )
      values ( s.employee_id, s.reviewer_id, s.review_cycle_id, s.rating, s.comments, s.review_date );
   end if;

   commit;
end;
/