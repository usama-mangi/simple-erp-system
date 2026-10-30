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
   id          number generated always as identity,
   name        varchar2(30) not null,
   code        varchar2(3) not null,
   location    varchar2(50) not null,
   established date not null,
   created_at  timestamp default systimestamp not null,
   updated_at  timestamp,
   constraint pk_department_id primary key ( id ),
   constraint uq_departments_name unique ( name ),
   constraint uq_departments_code unique ( code )
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

create table if not exists employment_details (
   id                   number generated always as identity,
   employee_id          number not null,
   job_title            varchar2(50) not null,
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