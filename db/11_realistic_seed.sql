-- 10_realistic_seed.sql
-- Large, realistic seed data for a mid-size company. Re-runnable: every
-- insert is guarded by natural-key existence checks where practical; the
-- script assumes a clean schema (00_drop .. 09_*) and is meant to replace
-- the tiny demo seed for load/UX testing.
--
-- Generates ~250 employees across a 15-department hierarchy, ~300
-- assignments, status history, manager history, leave balances/requests,
-- and app users. All inserts go through one transaction; commit at end.

-- =========================================================
-- LOOKUPS (idempotent)
-- =========================================================
merge into genders g using (select 'Male' title from dual union all select 'Female' from dual union all select 'Other' from dual) s on (g.title=s.title) when not matched then insert (title) values (s.title);
merge into employment_types t using (select 'Full-Time' title from dual union all select 'Part-Time' from dual union all select 'Contract' from dual union all select 'Intern' from dual) s on (t.title=s.title) when not matched then insert (title) values (s.title);
merge into employment_status es using (select 'Active' title from dual union all select 'On Leave' from dual union all select 'Notice Period' from dual union all select 'Suspended' from dual union all select 'Terminated' from dual union all select 'Resigned' from dual union all select 'Retired' from dual union all select 'Probation' from dual) s on (es.title=s.title) when not matched then insert (title) values (s.title);
insert into leave_statuses (title) select 'Pending' from dual where not exists (select 1 from leave_statuses where title='Pending');
merge into leave_statuses ls using (select 'Approved' title from dual union all select 'Rejected' from dual union all select 'Cancelled' from dual) s on (ls.title=s.title) when not matched then insert (title) values (s.title);
merge into leave_types lt using (select 'Annual' title from dual union all select 'Sick' from dual union all select 'Personal' from dual union all select 'Unpaid' from dual) s on (lt.title=s.title) when not matched then insert (title) values (s.title);
commit;

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
