drop index if exists uq_dept_current_manager;
drop index if exists uq_det_current_job;
drop index if exists uq_lr_open_request;

drop table if exists documents;
drop table if exists document_types;

drop table if exists status_events;
drop table if exists department_managers;
drop table if exists employment_details;
drop table if exists positions;

drop table if exists leave_balances;
drop table if exists leave_requests;
drop table if exists leave_types;
drop table if exists leave_statuses;

drop table if exists audit_log;
drop table if exists app_users;
drop table if exists employees;
drop table if exists departments;
drop table if exists employment_status;
drop table if exists employment_types;
drop table if exists genders;