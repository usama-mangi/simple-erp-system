create table if not exists genders (
   id    number generated always as identity,
   title varchar2(10) not null,
   constraint pk_gender_id primary key ( id ),
   constraint uq_genders_title unique ( title )
);

create table if not exists employment_types (
   id    number generated always as identity,
   title varchar2(20) not null,
   constraint pk_employment_type_id primary key ( id ),
   constraint uq_employment_types_title unique ( title )
);

create table if not exists employment_status (
   id    number generated always as identity,
   title varchar2(20) not null, -- Removed the redundant inline 'unique'
   constraint pk_employment_status_id primary key ( id ),
   constraint uq_employment_status_title unique ( title )
);

create table if not exists departments (
   id                   number generated always as identity,
   name                 varchar2(30) not null,
   code                 varchar2(3) not null,
   location             varchar2(50) not null,
   established          date not null,
   parent_department_id number, -- null = top-level department
   created_at           timestamp default systimestamp not null,
   updated_at           timestamp,
   constraint pk_department_id primary key ( id ),
   constraint uq_departments_name unique ( name ),
   constraint uq_departments_code unique ( code ),
   constraint fk_dept_parent foreign key ( parent_department_id )
      references departments ( id ),
   constraint chk_dept_parent_not_self check ( parent_department_id is null
       or parent_department_id != id )
);

create table if not exists employees (
   id            number generated always as identity,
   first_name    varchar2(50) not null,
   last_name     varchar2(50) not null,
   email         varchar2(100) not null,
   phone         varchar2(20) not null,
   date_of_birth date not null,
   gender_id     number not null,
   address       varchar2(100) not null,
   is_deleted    number(1) default 0 not null,
   created_at    timestamp default systimestamp not null,
   updated_at    timestamp,
   constraint pk_employee_id primary key ( id ),
   constraint fk_emp_gender foreign key ( gender_id )
      references genders ( id ), -- Fixed table if not exists name 'genders' and fixed constraint name
   constraint uq_employees_email unique ( email ),
   constraint uq_employees_phone unique ( phone )
);

create table if not exists positions (
   id         number generated always as identity,
   title      varchar2(50) not null,
   created_at timestamp default systimestamp not null,
   updated_at timestamp,
   constraint pk_position_id primary key ( id ),
   constraint uq_positions_title unique ( title )
);

create table if not exists employment_details (
   id                   number generated always as identity,
   employee_id          number not null,
   position_id          number not null,
   salary               number(10,2) not null,
   start_date           date not null,
   end_date             date,
   department_id        number not null,
   employment_type_id   number not null,
   employment_status_id number not null,
   created_at           timestamp default systimestamp not null,
   updated_at           timestamp,
   constraint pk_employee_detail_id primary key ( id ),
   constraint fk_det_employee foreign key ( employee_id )
      references employees ( id ), -- Unique constraint name
   constraint fk_det_position foreign key ( position_id )
      references positions ( id ),
   constraint fk_det_department foreign key ( department_id )
      references departments ( id ), -- Unique constraint name
   constraint fk_det_employment_type foreign key ( employment_type_id )
      references employment_types ( id ),
   constraint fk_det_employment_status foreign key ( employment_status_id )
      references employment_status ( id ),
   constraint chk_det_salary check ( salary >= 0 ),
   constraint chk_det_end_date check ( end_date is null
       or end_date > start_date ) -- Unique check constraint name
);

create table if not exists status_events (
   id              number generated always as identity,
   employee_id     number not null,
   from_status_id  number,
   to_status_id    number not null,
   changed_at      timestamp default systimestamp not null,
   note            varchar2(200),
   constraint pk_status_event_id primary key ( id ),
   constraint fk_se_employee foreign key ( employee_id )
      references employees ( id ),
   constraint fk_se_from_status foreign key ( from_status_id )
      references employment_status ( id ),
   constraint fk_se_to_status foreign key ( to_status_id )
      references employment_status ( id ),
   constraint chk_se_transition check ( from_status_id is null
       or from_status_id != to_status_id )
);

create index if not exists ix_se_employee_at
   on status_events ( employee_id, changed_at );

create table if not exists department_managers (
   id            number generated always as identity,
   department_id number not null,
   manager_id    number not null,
   start_date    date not null,
   end_date      date,
   created_at    timestamp default systimestamp not null,
   updated_at    timestamp,
   constraint pk_department_manager_id primary key ( id ),
   constraint fk_mgr_department foreign key ( department_id )
      references departments ( id ), -- Unique constraint name
   constraint fk_mgr_employee foreign key ( manager_id )
      references employees ( id ), -- Unique constraint name
   constraint chk_mgr_end_date check ( end_date is null
       or end_date > start_date ) -- Unique check constraint name
);

create table if not exists leave_types (
   id         number generated always as identity,
   title      varchar2(30) not null,
   created_at timestamp default systimestamp not null,
   updated_at timestamp,
   constraint pk_leave_type_id primary key ( id ),
   constraint uq_leave_types_title unique ( title )
);

create table if not exists leave_statuses (
   id         number generated always as identity,
   title      varchar2(20) not null,
   created_at timestamp default systimestamp not null,
   updated_at timestamp,
   constraint pk_leave_status_id primary key ( id ),
   constraint uq_leave_statuses_title unique ( title )
);

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
);

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
);

create unique index uq_det_current_job on
   employment_details (
      case
         when
            end_date
         is null then
               employee_id
         else
            null
      end
   );

create unique index uq_dept_current_manager on
   department_managers (
      case
         when
            end_date
         is null then
               manager_id
         else
            null
      end
   );

-- One open (Pending) leave request per employee, the leave-domain twin of
-- uq_det_current_job. The literal 1 is leave_statuses.Pending's id: 04_seed
-- inserts Pending first and guards that it lands on 1, because this index
-- expression cannot join to the statuses table.
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
   );

-- Case-insensitive uniqueness on top of the exact-match unique key:
-- Ora@x.com vs ora@x.com is one subscriber, not two.
create unique index uq_employees_email_lower on
   employees ( lower(email) );