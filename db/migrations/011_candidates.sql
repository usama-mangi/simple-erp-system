-- migrations/011_candidates.sql
-- One-shot migration for databases built before the candidates domain existed.
-- Guarded and re-runnable.

-- =========================================================
-- 1. TABLE
-- =========================================================

create table if not exists candidates (
   id         number generated always as identity,
   first_name varchar2(50) not null,
   last_name  varchar2(50) not null,
   email      varchar2(100) not null,
   phone      varchar2(20) not null,
   resume_url varchar2(500),
   source     varchar2(100), -- e.g., LinkedIn, Indeed, Referral
   status_id  number not null,
   created_at timestamp default systimestamp not null,
   updated_at timestamp,
   constraint pk_candidate_id primary key ( id ),
   constraint uq_candidates_email unique ( email ),
   constraint fk_cand_status foreign key ( status_id )
      references application_stages ( id ),
   constraint chk_cand_email check ( regexp_like ( email,
                                                   '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$' ) )
)
/

-- =========================================================
-- 2. SEED CANDIDATES (sample data)
-- =========================================================

merge into candidates c
using (
   select 'John' first_name,
          'Doe' last_name,
          'john.doe@example.com' email,
          '555-1234' phone,
          null resume_url,
          'LinkedIn' source,
          1 status_id
     from dual
   union all
   select 'Jane' first_name,
          'Smith' last_name,
          'jane.smith@example.com' email,
          '555-5678' phone,
          '/resumes/jane_resume.pdf',
          'Indeed',
          1 status_id
     from dual
   union all
   select 'Bob' first_name,
          'Johnson' last_name,
          'bob.johnson@example.com' email,
          '555-9012' phone,
          '/resumes/bob_cv.pdf',
          'Referral',
          1 status_id
     from dual
) s on ( c.email = s.email )
when not matched then
insert (
   first_name,
   last_name,
   email,
   phone,
   resume_url,
   source,
   status_id )
values
   ( s.first_name,
     s.last_name,
     s.email,
     s.phone,
     s.resume_url,
     s.source,
     s.status_id )
/

commit;

-- =========================================================
-- 3. VERIFICATION
-- =========================================================

select table_name
  from user_tables
 where table_name = 'CANDIDATES'
/

select id,
       first_name
       || ' '
       || last_name as full_name,
       email,
       source,
       s.title as status
  from candidates c
  join application_stages s
on s.id = c.status_id
/