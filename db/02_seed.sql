-- =========================================================
-- 1. SEED GENDERS
-- =========================================================

merge into genders g
using (
   select 'Male' title
     from dual
   union all
   select 'Female'
     from dual
   union all
   select 'Other'
     from dual
) s on ( g.title = s.title )
when not matched then
insert ( title )
values
   ( s.title );


-- =========================================================
-- 2. SEED EMPLOYMENT TYPES
-- =========================================================

merge into employment_types t
using (
   select 'Full-Time' title
     from dual
   union all
   select 'Part-Time'
     from dual
   union all
   select 'Contract'
     from dual
   union all
   select 'Seasonal'
     from dual
   union all
   select 'Intern'
     from dual
) s on ( t.title = s.title )
when not matched then
insert ( title )
values
   ( s.title );


-- =========================================================
-- 3. SEED EMPLOYMENT STATUSES
-- =========================================================

merge into employment_status es
using (
   select 'Active' title
     from dual
   union all
   select 'On Leave'
     from dual
   union all
   select 'Notice Period'
     from dual
   union all
   select 'Suspended'
     from dual
   union all
   select 'Terminated'
     from dual
   union all
   select 'Resigned'
     from dual
   union all
   select 'Retired'
     from dual
   union all
   select 'Probation'
     from dual
) s on ( es.title = s.title )
when not matched then
insert ( title )
values
   ( s.title );


-- =========================================================
-- 4. SEED DEPARTMENTS
-- =========================================================

merge into departments d
using (
   select 'Human Resources' name,
          'HR' code,
          'Lahore' location,
          date '2015-01-01' established
     from dual
   union all
   select 'Information Technology',
          'IT',
          'Lahore',
          date '2016-03-15'
     from dual
   union all
   select 'Finance',
          'FIN',
          'Karachi',
          date '2014-06-01'
     from dual
   union all
   select 'Marketing',
          'MKT',
          'Islamabad',
          date '2017-09-01'
     from dual
   union all
   select 'Operations',
          'OPS',
          'Lahore',
          date '2018-02-01'
     from dual
) s on ( d.name = s.name )
when not matched then
insert (
   name,
   code,
   location,
   established )
values
   ( s.name,
     s.code,
     s.location,
     s.established );


-- =========================================================
-- 5. SEED EMPLOYEES
-- =========================================================

merge into employees e
using (
   select 'Ali' first_name,
          'Khan' last_name,
          'ali.khan@example.com' email,
          '+923001000001' phone,
          date '1990-05-12' dob,
          'Male' gender,
          'Gulberg, Lahore' address
     from dual
   union all
   select 'Ayesha',
          'Ahmed',
          'ayesha.ahmed@example.com',
          '+923001000002',
          date '1993-08-21',
          'Female',
          'DHA, Lahore'
     from dual
   union all
   select 'Usman',
          'Malik',
          'usman.malik@example.com',
          '+923001000003',
          date '1988-11-03',
          'Male',
          'Johar Town, Lahore'
     from dual
   union all
   select 'Fatima',
          'Sheikh',
          'fatima.sheikh@example.com',
          '+923001000004',
          date '1995-02-17',
          'Female',
          'Clifton, Karachi'
     from dual
   union all
   select 'Hamza',
          'Butt',
          'hamza.butt@example.com',
          '+923001000005',
          date '1992-07-09',
          'Male',
          'Model Town, Lahore'
     from dual
   union all
   select 'Sara',
          'Iqbal',
          'sara.iqbal@example.com',
          '+923001000006',
          date '1996-12-25',
          'Female',
          'F-10, Islamabad'
     from dual
   union all
   select 'Bilal',
          'Hussain',
          'bilal.hussain@example.com',
          '+923001000007',
          date '1985-04-30',
          'Male',
          'Gulshan, Karachi'
     from dual
   union all
   select 'Zainab',
          'Raza',
          'zainab.raza@example.com',
          '+923001000008',
          date '1998-09-14',
          'Female',
          'Bahria Town, Lahore'
     from dual
) s on ( e.email = s.email )
when not matched then
insert (
   first_name,
   last_name,
   email,
   phone,
   date_of_birth,
   gender_id,
   address )
values
   ( s.first_name,
     s.last_name,
     s.email,
     s.phone,
     s.dob,
     (
        select g.id
          from genders g
         where g.title = s.gender
     ),
     s.address );


-- =========================================================
-- 6. SEED EMPLOYMENT DETAILS
--    Includes current and historical employment records.
-- =========================================================

merge into employment_details ed
using (
   select 'ali.khan@example.com' email,
          'IT' dept_code,
          'Software Engineer' job_title,
          150000 salary,
          date '2022-01-10' start_date,
          cast(null as date) end_date,
          'Full-Time' emp_type,
          'Active' emp_status
     from dual
   union all
   select 'ayesha.ahmed@example.com',
          'HR',
          'HR Manager',
          180000,
          date '2021-04-01',
          cast(null as date),
          'Full-Time',
          'Active'
     from dual
   union all
   select 'usman.malik@example.com',
          'FIN',
          'Financial Analyst',
          140000,
          date '2023-02-15',
          cast(null as date),
          'Full-Time',
          'Active'
     from dual
   union all
   select 'fatima.sheikh@example.com',
          'MKT',
          'Marketing Specialist',
          120000,
          date '2022-07-01',
          cast(null as date),
          'Full-Time',
          'Active'
     from dual
   union all
   select 'hamza.butt@example.com',
          'IT',
          'IT Support Engineer',
          95000,
          date '2024-01-15',
          cast(null as date),
          'Full-Time',
          'Active'
     from dual
   union all
   select 'sara.iqbal@example.com',
          'MKT',
          'Marketing Intern',
          40000,
          date '2025-06-01',
          cast(null as date),
          'Intern',
          'Probation'
     from dual
   union all
   select 'bilal.hussain@example.com',
          'OPS',
          'Operations Manager',
          200000,
          date '2020-03-01',
          cast(null as date),
          'Full-Time',
          'Active'
     from dual
   union all
   select 'zainab.raza@example.com',
          'HR',
          'HR Assistant',
          80000,
          date '2024-08-01',
          cast(null as date),
          'Full-Time',
          'Active'
     from dual
   union all
    -- Historical job for Ali
   select 'ali.khan@example.com',
          'IT',
          'Junior Developer',
          80000,
          date '2019-01-01',
          date '2021-12-31',
          'Full-Time',
          'Resigned'
     from dual
   union all
    -- Historical job for Fatima
   select 'fatima.sheikh@example.com',
          'MKT',
          'Marketing Assistant',
          70000,
          date '2020-01-01',
          date '2022-06-30',
          'Full-Time',
          'Resigned'
     from dual
) s on ( ed.employee_id = (
      select e.id
        from employees e
       where e.email = s.email
   )
   and ed.start_date = s.start_date
   and ed.job_title = s.job_title )
when not matched then
insert (
   employee_id,
   job_title,
   salary,
   start_date,
   end_date,
   department_id,
   employment_type_id,
   employment_status_id )
values
   ( (
      select e.id
        from employees e
       where e.email = s.email
   ),
     s.job_title,
     s.salary,
     s.start_date,
     s.end_date,
     (
        select d.id
          from departments d
         where d.code = s.dept_code
     ),
     (
        select et.id
          from employment_types et
         where et.title = s.emp_type
     ),
     (
        select es.id
          from employment_status es
         where es.title = s.emp_status
     ) );


-- =========================================================
-- 7. SEED DEPARTMENT MANAGERS
-- =========================================================

merge into department_managers dm
using (
   select 'HR' dept_code,
          'ayesha.ahmed@example.com' email,
          date '2021-04-01' start_date
     from dual
   union all
   select 'IT',
          'ali.khan@example.com',
          date '2022-01-10'
     from dual
   union all
   select 'FIN',
          'usman.malik@example.com',
          date '2023-02-15'
     from dual
   union all
   select 'OPS',
          'bilal.hussain@example.com',
          date '2020-03-01'
     from dual
) s on ( dm.department_id = (
      select d.id
        from departments d
       where d.code = s.dept_code
   )
   and dm.manager_id = (
   select e.id
     from employees e
    where e.email = s.email
)
   and dm.start_date = s.start_date )
when not matched then
insert (
   department_id,
   manager_id,
   start_date,
   end_date )
values
   ( (
      select d.id
        from departments d
       where d.code = s.dept_code
   ),
     (
        select e.id
          from employees e
         where e.email = s.email
     ),
     s.start_date,
     null );


-- =========================================================
-- 8. VERIFY SEEDED DATA
-- =========================================================

select 'Genders' table_name,
       count(*) total_rows
  from genders
union all
select 'Employment Types',
       count(*)
  from employment_types
union all
select 'Employment Status',
       count(*)
  from employment_status
union all
select 'Departments',
       count(*)
  from departments
union all
select 'Employees',
       count(*)
  from employees
union all
select 'Employment Details',
       count(*)
  from employment_details
union all
select 'Department Managers',
       count(*)
  from department_managers;