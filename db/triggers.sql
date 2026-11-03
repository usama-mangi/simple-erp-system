create or replace trigger trg_set_dept_updated_at before
   update on departments
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/

create or replace trigger trg_set_emp_updated_at before
   update on employees
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/

create or replace trigger trg_set_det_updated_at before
   update on employment_details
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/

create or replace trigger trg_set_dept_mgr_updated_at before
   update on department_managers
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/

create or replace trigger trg_set_lr_updated_at before
   update on leave_requests
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/

create or replace trigger trg_set_bal_updated_at before
   update on leave_balances
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/
create or replace trigger trg_audit_employees
after insert or update or delete on employees
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('EMPLOYEES', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('EMPLOYEES', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('EMPLOYEES', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/

create or replace trigger trg_audit_departments
after insert or update or delete on departments
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DEPARTMENTS', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DEPARTMENTS', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DEPARTMENTS', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/

create or replace trigger trg_audit_employment_details
after insert or update or delete on employment_details
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('EMPLOYMENT_DETAILS', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('EMPLOYMENT_DETAILS', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('EMPLOYMENT_DETAILS', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/

create or replace trigger trg_audit_department_managers
after insert or update or delete on department_managers
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DEPARTMENT_MANAGERS', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DEPARTMENT_MANAGERS', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DEPARTMENT_MANAGERS', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/

create or replace trigger trg_audit_leave_requests
after insert or update or delete on leave_requests
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('LEAVE_REQUESTS', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('LEAVE_REQUESTS', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('LEAVE_REQUESTS', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/

create or replace trigger trg_audit_leave_balances
after insert or update or delete on leave_balances
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('LEAVE_BALANCES', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('LEAVE_BALANCES', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('LEAVE_BALANCES', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/

create or replace trigger trg_audit_app_users
after insert or update or delete on app_users
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('APP_USERS', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('APP_USERS', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('APP_USERS', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/
create or replace trigger trg_audit_document_types
after insert or update or delete on document_types
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENT_TYPES', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENT_TYPES', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENT_TYPES', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/

create or replace trigger trg_audit_documents
after insert or update or delete on documents
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENTS', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENTS', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENTS', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/
create or replace trigger trg_set_documents_updated_at before
   update on documents
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/
