-- migrations/010_requisitions.sql
-- One-shot migration for databases built before the requisitions domain existed.
-- Guarded and re-runnable.

-- =========================================================
-- 1. TABLE
-- =========================================================

create table if not exists requisitions (
   id            number generated always as identity,
   title         varchar2(100) not null,
   description   varchar2(500),
   department_id number not null,
   position_id   number not null,
   salary        number(10,2) not null,
   openings      number(5) default 1 not null,
   status_id     number not null,
   created_at    timestamp default systimestamp not null,
   updated_at    timestamp,
   constraint pk_requisition_id primary key ( id ),
   constraint uq_requisitions_title unique ( title ),
   constraint fk_req_dept foreign key ( department_id )
      references departments ( id ),
   constraint fk_req_pos foreign key ( position_id )
      references positions ( id ),
   constraint fk_req_status foreign key ( status_id )
      references application_stages ( id ),
   constraint chk_req_salary check ( salary >= 0 ),
   constraint chk_req_openings check ( openings > 0 )
)
/

-- =========================================================
-- 2. SEED REQUISITIONS (sample data)
-- =========================================================

merge into requisitions r
using (
   select 'Software Engineer' title,
          'Backend position' description,
          1 department_id,
          1 position_id,
          8000 salary,
          2 openings,
          1 status_id
     from dual
   union all
   select 'Data Analyst',
          'Analytics position' description,
          2 department_id,
          2 position_id,
          6500 salary,
          1 openings,
          1 status_id
     from dual
   union all
   select 'Product Manager',
          'Product leadership' description,
          1 department_id,
          3 position_id,
          12000 salary,
          1 openings,
          1 status_id
     from dual
) s on ( r.title = s.title )
when not matched then
insert (
   title,
   description,
   department_id,
   position_id,
   salary,
   openings,
   status_id )
values
   ( s.title,
     s.description,
     s.department_id,
     s.position_id,
     s.salary,
     s.openings,
     s.status_id )
/

commit;

-- =========================================================
-- 3. VERIFICATION
-- =========================================================

select table_name
  from user_tables
 where table_name = 'REQUISITIONS'
/

select r.id,
       r.title,
       d.name as department,
       p.title as position,
       r.salary,
       r.openings,
       s.title as status
  from requisitions r
  join departments d
on d.id = r.department_id
  join positions p
on p.id = r.position_id
  join application_stages s
on s.id = r.status_id
/