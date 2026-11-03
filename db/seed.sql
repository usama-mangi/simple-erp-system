-- SmartERP seed. One script builds the whole dataset, in dependency order:
--   1. lookups (genders, employment types/status, leave statuses/types, document types)
--   2. sample world (small hand-authored cast: ali.khan, ayesha.ahmed, ...)
--   3. realistic bulk (250 generated employees across a 15-department hierarchy)
--   4. document fixtures (dates relative to sysdate)
--
-- Runs only on a CLEAN schema (see rebuild.sql). Populated databases must use
-- upgrade.sql, which runs migrations/ and never reloads data.

-- =========================================================
-- LOOKUPS (idempotent)
-- =========================================================
merge into genders g using (select 'Male' title from dual union all select 'Female' from dual union all select 'Other' from dual) s on (g.title=s.title) when not matched then insert (title) values (s.title);
merge into employment_types t using (select 'Full-Time' title from dual union all select 'Part-Time' from dual union all select 'Contract' from dual union all select 'Intern' from dual) s on (t.title=s.title) when not matched then insert (title) values (s.title);
merge into employment_status es using (select 'Active' title from dual union all select 'On Leave' from dual union all select 'Notice Period' from dual union all select 'Suspended' from dual union all select 'Terminated' from dual union all select 'Resigned' from dual union all select 'Retired' from dual union all select 'Probation' from dual) s on (es.title=s.title) when not matched then insert (title) values (s.title);
insert into leave_statuses (title) select 'Pending' from dual where not exists (select 1 from leave_statuses where title='Pending');
merge into leave_statuses ls using (select 'Approved' title from dual union all select 'Rejected' from dual union all select 'Cancelled' from dual) s on (ls.title=s.title) when not matched then insert (title) values (s.title);
merge into leave_types lt using (select 'Annual' title from dual union all select 'Sick' from dual union all select 'Personal' from dual union all select 'Unpaid' from dual) s on (lt.title=s.title) when not matched then insert (title) values (s.title);
merge into document_types dt using (
   select 'Employment Contract' title from dual union all select 'Passport' from dual
   union all select 'National ID' from dual union all select 'Visa' from dual
   union all select 'Certification' from dual union all select 'Medical' from dual
   union all select 'Other' from dual
) s on (dt.title=s.title) when not matched then insert (title) values (s.title);
commit;

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
-- 6. SEED POSITIONS
--    One master row per job title; employment_details points
--    at positions, the free-text job_title column is gone.
-- =========================================================

merge into positions p
using (
   select 'Software Engineer' title
     from dual
   union all
   select 'HR Manager'
     from dual
   union all
   select 'Financial Analyst'
     from dual
   union all
   select 'Marketing Specialist'
     from dual
   union all
   select 'IT Support Engineer'
     from dual
   union all
   select 'Marketing Intern'
     from dual
   union all
   select 'Operations Manager'
     from dual
   union all
   select 'HR Assistant'
     from dual
   union all
   select 'Junior Developer'
     from dual
   union all
   select 'Marketing Assistant'
     from dual
) s on ( p.title = s.title )
when not matched then
insert ( title )
values
   ( s.title );


-- =========================================================
-- 7. SEED EMPLOYMENT DETAILS
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
   and ed.position_id = (
   select p.id
     from positions p
    where p.title = s.job_title
) )
when not matched then
insert (
   employee_id,
   position_id,
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
     (
        select p.id
          from positions p
         where p.title = s.job_title
     ),
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
-- 7a. SEED STATUS EVENTS (backfill from assignment history)
-- =========================================================

insert into status_events (
   employee_id,
   from_status_id,
   to_status_id,
   changed_at,
   note
)
   select ed.employee_id,
          lag(ed.employment_status_id)
          over(partition by ed.employee_id
               order by ed.start_date,
                        ed.id
          ),
          ed.employment_status_id,
          cast(ed.start_date as timestamp),
          'Backfilled from assignment history'
     from employment_details ed
    where not exists (
      select 1
        from status_events se
       where se.employee_id = ed.employee_id
   );

-- =========================================================
-- 8. SEED DEPARTMENT MANAGERS
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
-- 11. SEED LEAVE BALANCES
--    used_days starts at 0 and is synced from the approved
--    sample requests in section 13, so the balance rows and the
--    request rows can never disagree.
-- =========================================================

merge into leave_balances lb
using (
   select 'ali.khan@example.com' email,
          'Annual' type_title,
          20 entitlement
     from dual
   union all
   select 'ayesha.ahmed@example.com',
          'Sick',
          10
     from dual
   union all
   select 'usman.malik@example.com',
          'Annual',
          20
     from dual
) s on ( lb.employee_id = (
      select e.id
        from employees e
       where e.email = s.email
   )
   and lb.leave_type_id = (
   select lt.id
     from leave_types lt
    where lt.title = s.type_title
)
   and lb.year = extract(year from sysdate) )
when not matched then
insert (
   employee_id,
   leave_type_id,
   year,
   entitlement_days,
   used_days )
values
   ( (
      select e.id
        from employees e
       where e.email = s.email
   ),
     (
        select lt.id
          from leave_types lt
         where lt.title = s.type_title
     ),
     extract(year from sysdate),
     s.entitlement,
     0 );


-- =========================================================
-- 12. SEED LEAVE REQUESTS
--    One approved request covering today (drives the dashboard's
--    On-leave-today card), one approved earlier this month (drives
--    the absence stats), one Pending (exercises the open-request
--    index). Dates are sysdate-relative on purpose: the samples
--    stay meaningful on any rebuild date.
-- =========================================================

merge into leave_requests lr
using (
   select 'ali.khan@example.com' email,
          'Annual' type_title,
          trunc(sysdate) - 2 start_date,
          trunc(sysdate) + 1 end_date,
          'Approved' status,
          'Long weekend away' reason
     from dual
   union all
   select 'ayesha.ahmed@example.com',
          'Sick',
          trunc(
             sysdate,
             'MM'
          ),
          least(
             trunc(
                sysdate,
                'MM'
             ) + 2,
             trunc(sysdate)
          ),
          'Approved',
          'Flu'
     from dual
   union all
   select 'fatima.sheikh@example.com',
          'Personal',
          trunc(sysdate) + 7,
          trunc(sysdate) + 8,
          'Pending',
          'Family event'
     from dual
) s on ( lr.employee_id = (
      select e.id
        from employees e
       where e.email = s.email
   )
   and lr.leave_type_id = (
   select lt.id
     from leave_types lt
    where lt.title = s.type_title
)
   and lr.start_date = s.start_date )
when not matched then
insert (
   employee_id,
   leave_type_id,
   status_id,
   start_date,
   end_date,
   reason,
   decided_at )
values
   ( (
      select e.id
        from employees e
       where e.email = s.email
   ),
     (
        select lt.id
          from leave_types lt
         where lt.title = s.type_title
     ),
     (
        select ls.id
          from leave_statuses ls
         where ls.title = s.status
     ),
     s.start_date,
     s.end_date,
     s.reason,
     case
        when s.status = 'Pending' then
            null
        else
           systimestamp
     end
   );


-- =========================================================
-- 13. SYNC BALANCES FROM APPROVED REQUESTS
--    used_days is the sum of inclusive calendar days of every
--    approved request whose start year matches the balance year.
--    This is the same rule the app's approve path applies, so a
--    rebuilt database starts with balances that agree with its
--    request history.
-- =========================================================

update leave_balances lb
   set
   used_days = (
      select nvl(
         sum(lr.end_date - lr.start_date + 1),
         0
      )
        from leave_requests lr
       where lr.employee_id = lb.employee_id
         and lr.leave_type_id = lb.leave_type_id
         and lr.status_id = (
         select ls.id
           from leave_statuses ls
          where ls.title = 'Approved'
      )
         and extract(year from lr.start_date) = lb.year
   );



-- =========================================================
-- 9. SEED APP USERS
-- =========================================================

merge into app_users au
using (
   select 'ali.khan@example.com' email,
          'ali.khan' username,
          '/gQlnJhWSDE5dgpujA96Mg==:2Q3q3jM/yGT0E06oRoHKL7UpHYJXppqRBiJoHF3ksfc=' password_hash
     from dual
) s on ( au.username = s.username )
when not matched then
insert (
   employee_id,
   username,
   password_hash,
   role,
   is_active )
values
   ( (
      select e.id
        from employees e
       where e.email = s.email
   ),
     s.username,
     s.password_hash,
     'Admin',
     1 );

-- =========================================================
-- BULK SEED
-- =========================================================
declare
   type num_tab is table of number index by pls_integer;
   type str_tab is table of varchar2(100) index by pls_integer;

   first_names str_tab;
   last_names  str_tab;
   pos_titles  str_tab;

   dept_ids    num_tab;
   pos_ids     num_tab;
   emp_ids     num_tab;
   lt_ids      num_tab;
   status_active   number;
   status_onleave  number;
   status_probation number;
   pending_id      number;
   approved_id     number;
   rejected_id     number;
   cancelled_id    number;

   n_emp constant pls_integer := 250;
   n_dept constant pls_integer := 15;
   n_pos  constant pls_integer := 18;

   function pick (arr in str_tab, i in pls_integer) return varchar2 is
   begin
      return arr(mod(i-1, arr.count) + 1);
   end;

begin
   -- name pools
   first_names(1):='James'; first_names(2):='Mary'; first_names(3):='Robert'; first_names(4):='Patricia';
   first_names(5):='John'; first_names(6):='Jennifer'; first_names(7):='Michael'; first_names(8):='Linda';
   first_names(9):='David'; first_names(10):='Elizabeth'; first_names(11):='William'; first_names(12):='Barbara';
   first_names(13):='Richard'; first_names(14):='Susan'; first_names(15):='Joseph'; first_names(16):='Jessica';
   first_names(17):='Thomas'; first_names(18):='Sarah'; first_names(19):='Charles'; first_names(20):='Karen';
   last_names(1):='Smith'; last_names(2):='Johnson'; last_names(3):='Williams'; last_names(4):='Brown';
   last_names(5):='Jones'; last_names(6):='Garcia'; last_names(7):='Miller'; last_names(8):='Davis';
   last_names(9):='Rodriguez'; last_names(10):='Martinez'; last_names(11):='Hernandez'; last_names(12):='Lopez';
   last_names(13):='Gonzalez'; last_names(14):='Wilson'; last_names(15):='Anderson'; last_names(16):='Thomas';
   last_names(17):='Taylor'; last_names(18):='Moore'; last_names(19):='Jackson'; last_names(20):='Martin';
   pos_titles(1):='Software Engineer'; pos_titles(2):='Senior Software Engineer'; pos_titles(3):='Engineering Manager';
   pos_titles(4):='Product Manager'; pos_titles(5):='UX Designer'; pos_titles(6):='QA Engineer';
   pos_titles(7):='DevOps Engineer'; pos_titles(8):='Data Analyst'; pos_titles(9):='HR Specialist';
   pos_titles(10):='HR Manager'; pos_titles(11):='Finance Analyst'; pos_titles(12):='Accountant';
   pos_titles(13):='Sales Representative'; pos_titles(14):='Sales Manager'; pos_titles(15):='Marketing Specialist';
   pos_titles(16):='Operations Manager'; pos_titles(17):='Customer Support'; pos_titles(18):='IT Administrator';

   -- status ids
   select id into status_active from employment_status where title='Active';
   select id into status_onleave from employment_status where title='On Leave';
   select id into status_probation from employment_status where title='Probation';
   select id into pending_id from leave_statuses where title='Pending';
   select id into approved_id from leave_statuses where title='Approved';
   select id into rejected_id from leave_statuses where title='Rejected';
   select id into cancelled_id from leave_statuses where title='Cancelled';

   -- leave type ids
   for r in (select id from leave_types) loop
      lt_ids(lt_ids.count+1) := r.id;
   end loop;

   -- departments: 3 parents + 12 children
   for i in 1..3 loop
      insert into departments (name, code, location, established, parent_department_id)
      values ('Division '||i, 'DV'||i, 'Headquarters Floor '||i, date '2005-01-01', null)
      returning id into dept_ids(i);
   end loop;
   for i in 1..12 loop
      insert into departments (name, code, location, established, parent_department_id)
      values ('Team '||i, 'T'||lpad(i,2,'0'), 'Branch Office '||i, date '2010-06-15', dept_ids(mod(i-1,3)+1))
      returning id into dept_ids(3+i);
   end loop;

   -- positions (idempotent)
   for i in 1..n_pos loop
      insert into positions (title)
      select pos_titles(i) from dual
       where not exists (select 1 from positions where title = pos_titles(i));
      select id into pos_ids(i) from positions where title = pos_titles(i);
   end loop;

   -- employees + current assignments + status events
   for i in 1..n_emp loop
      declare
         v_fn varchar2(50) := first_names(mod(i-1,20)+1);
         v_ln varchar2(50) := last_names(mod(i-1,20)+1);
      begin
      insert into employees (first_name, last_name, email, phone, date_of_birth, gender_id, address, is_deleted)
      values (
         v_fn,
         v_ln,
         lower(v_fn)||'.'||lower(v_ln)||i||'@example.com',
         '555'||lpad(i,7,'0'),
         date '1975-01-01' + mod(i*37, 9000),
         mod(i,2)+1,
         (100 + mod(i,900))||' '||v_ln||' St, City '||mod(i,40),
         0
      )
      returning id into emp_ids(i);

      -- current assignment
      insert into employment_details (employee_id, position_id, salary, start_date, end_date, department_id, employment_type_id, employment_status_id)
      values (
         emp_ids(i),
         pos_ids(mod(i-1, n_pos)+1),
         35000 + (mod(i-1, n_dept)+1)*4000 + (mod(i-1, n_pos)+1)*2500 + mod(i*97, 12000),
         add_months(trunc(sysdate, 'MM'), -mod(i, 24)) + mod(i, 27),
         null,
         dept_ids(mod(i-1, n_dept)+1),
         case when mod(i,20)=0 then 5 when mod(i,10)=0 then 4 when mod(i,7)=0 then 3 when mod(i,5)=0 then 2 else 1 end, -- Intern/Seasonal/Contract/Part-Time/Full-Time
         case when mod(i,13)=0 then status_probation else status_active end
      );

      -- status event for current assignment
      insert into status_events (employee_id, from_status_id, to_status_id, changed_at, note)
      values (emp_ids(i), null, case when mod(i,13)=0 then status_probation else status_active end, systimestamp - mod(i,30), 'Hired');

      end;

      -- previous closed assignment for ~1/4 of employees
      if mod(i,4)=0 then
         insert into employment_details (employee_id, position_id, salary, start_date, end_date, department_id, employment_type_id, employment_status_id)
         values (
            emp_ids(i),
            pos_ids(mod(i+5, n_pos)+1),
            30000 + (mod(i+7, n_dept)+1)*3000 + mod(i*53, 10000),
            add_months(trunc(sysdate, 'MM'), -mod(i, 24) - 6),
            add_months(trunc(sysdate, 'MM'), -mod(i, 24) - 3),
            dept_ids(mod(i+7, n_dept)+1),
            1,
            (select id from employment_status where title='Terminated')
         );
         insert into status_events (employee_id, from_status_id, to_status_id, changed_at, note)
         values (emp_ids(i), status_active, (select id from employment_status where title='Terminated'), systimestamp - 400, 'Left previous role');
      end if;
   end loop;

   -- department managers: one current manager per department (distinct employees)
   for d in 1..n_dept loop
      insert into department_managers (department_id, manager_id, start_date, end_date)
      values (dept_ids(d), emp_ids(d), date '2020-01-01', null);
      -- history: a previous manager for even departments
      if mod(d,2)=0 then
         insert into department_managers (department_id, manager_id, start_date, end_date)
         values (dept_ids(d), emp_ids(d+30), date '2018-01-01', date '2019-12-31');
      end if;
   end loop;

   -- leave balances: 2 years x all types per employee
   for i in 1..n_emp loop
      for j in 1..lt_ids.count loop
         for yr in 2025..2026 loop
            insert into leave_balances (employee_id, leave_type_id, year, entitlement_days, used_days)
            values (
               emp_ids(i),
               lt_ids(j),
               yr,
               case j when 1 then 20 when 2 then 10 when 3 then 5 else 12 end,
               0
            );
         end loop;
      end loop;
   end loop;

   -- leave requests covering all statuses
   for i in 1..n_emp loop
      -- Pending (one per 5 employees) -> satisfies one-open-request index
      if mod(i,5)=0 then
         insert into leave_requests (employee_id, leave_type_id, status_id, start_date, end_date, reason, decided_at)
         values (emp_ids(i), lt_ids(1), pending_id, trunc(sysdate)+10, trunc(sysdate)+14, 'Family vacation', null);
      end if;
      -- Approved
      if mod(i,3)=0 then
         insert into leave_requests (employee_id, leave_type_id, status_id, start_date, end_date, reason, decided_at)
         values (emp_ids(i), lt_ids(2), approved_id, trunc(sysdate)-40, trunc(sysdate)-36, 'Medical', systimestamp-35);
      end if;
      -- Rejected
      if mod(i,7)=0 then
         insert into leave_requests (employee_id, leave_type_id, status_id, start_date, end_date, reason, decided_at)
         values (emp_ids(i), lt_ids(3), rejected_id, trunc(sysdate)-20, trunc(sysdate)-18, 'Personal errand', systimestamp-19);
      end if;
      -- Cancelled
      if mod(i,11)=0 then
         insert into leave_requests (employee_id, leave_type_id, status_id, start_date, end_date, reason, decided_at)
         values (emp_ids(i), lt_ids(4), cancelled_id, trunc(sysdate)-10, trunc(sysdate)-9, 'Plans changed', systimestamp-9);
      end if;
   end loop;

   -- app users
   insert into app_users (employee_id, username, password_hash, role, is_active)
   values (emp_ids(1), 'admin', '/gQlnJhWSDE5dgpujA96Mg==:2Q3q3jM/yGT0E06oRoHKL7UpHYJXppqRBiJoHF3ksfc=', 'Admin', 1);
   for i in 2..6 loop
      insert into app_users (employee_id, username, password_hash, role, is_active)
      values (emp_ids(i), 'hr'||i, '/gQlnJhWSDE5dgpujA96Mg==:2Q3q3jM/yGT0E06oRoHKL7UpHYJXppqRBiJoHF3ksfc=', 'HR', 1);
   end loop;
   for i in 7..12 loop
      insert into app_users (employee_id, username, password_hash, role, is_active)
      values (emp_ids(i), 'viewer'||i, '/gQlnJhWSDE5dgpujA96Mg==:2Q3q3jM/yGT0E06oRoHKL7UpHYJXppqRBiJoHF3ksfc=', 'Viewer', 1);
   end loop;

   commit;
   dbms_output.put_line('Seeded '||n_emp||' employees across '||n_dept||' departments.');
end;
/

-- =========================================================
-- DOCUMENT FIXTURES
-- A few documents per sample employee, expiries relative to
-- sysdate: one already expired, two due within 30 days (drives
-- the dashboard watchlist), the rest far out or never.
-- =========================================================

merge into documents d
using (
   select 'ali.khan@example.com'    email, 'Passport'           type_title, 'Passport'              title, date '2021-03-01' issued_on, trunc(sysdate) + 12  expires_at, 'Renewed on last trek' note from dual
   union all
   select 'fatima.sheikh@example.com',     'Certification',      'AWS Solutions Architect',            date '2023-06-15',            trunc(sysdate) - 40,  'Lapsed, retake booked'
   union all
   select 'fatima.sheikh@example.com',     'Visa',               'Work visa (UK)',                    date '2024-01-20',            trunc(sysdate) + 26,  'Renewal filed with consulate'
   union all
   select 'hamza.butt@example.com',        'Employment Contract', 'Employment contract 2024',          date '2024-02-01',            null,                'Open-ended'
   union all
   select 'ayesha.ahmed@example.com',      'National ID',        'National ID card',                  date '2019-09-01',            trunc(sysdate) + 320, null
   union all
   select 'usman.malik@example.com',       'Medical',            'Medical fitness certificate',       date '2025-11-01',            trunc(sysdate) + 18,  'Annual checkup'
) s on (
   d.employee_id = (select e.id from employees e where e.email = s.email)
   and d.title = s.title
)
when not matched then
insert ( employee_id, type_id, title, issued_on, expires_at, note )
values (
   (select e.id from employees e where e.email = s.email),
   (select t.id from document_types t where t.title = s.type_title),
   s.title, s.issued_on, s.expires_at, s.note );
commit;

-- =========================================================
-- FINAL COUNTS
-- =========================================================

select 'Genders' table_name, count(*) total_rows from genders
union all select 'Employment Types', count(*) from employment_types
union all select 'Employment Status', count(*) from employment_status
union all select 'Departments', count(*) from departments
union all select 'Positions', count(*) from positions
union all select 'Employees', count(*) from employees
union all select 'Employment Details', count(*) from employment_details
union all select 'Status Events', count(*) from status_events
union all select 'Department Managers', count(*) from department_managers
union all select 'Leave Statuses', count(*) from leave_statuses
union all select 'Leave Types', count(*) from leave_types
union all select 'Leave Requests', count(*) from leave_requests
union all select 'Leave Balances', count(*) from leave_balances
union all select 'Document Types', count(*) from document_types
union all select 'Documents', count(*) from documents
union all select 'App Users', count(*) from app_users
/
