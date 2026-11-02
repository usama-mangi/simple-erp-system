-- 10_app_users_audit.sql
-- One-shot migration for databases built before app_users/audit_log existed.
-- Guarded and re-runnable.

create table if not exists app_users (
   id            number generated always as identity,
   employee_id   number not null,
   username      varchar2(50) not null,
   password_hash varchar2(256) not null,
   role          varchar2(20) not null,
   is_active     number(1) default 1 not null,
   created_at    timestamp default systimestamp not null,
   updated_at    timestamp,
   constraint pk_app_user_id primary key ( id )
)
/

create table if not exists audit_log (
   id          number generated always as identity,
   table_name  varchar2(30) not null,
   pk_value    varchar2(64) not null,
   action      varchar2(10) not null,
   changed_by  varchar2(64) not null,
   changed_at  timestamp default systimestamp not null,
   constraint pk_audit_log_id primary key ( id )
)
/

commit
/
select count(*) from app_users;
select count(*) from audit_log;
/