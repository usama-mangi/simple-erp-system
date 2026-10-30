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