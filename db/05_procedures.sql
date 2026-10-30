-- =========================================================
-- SP_ASSIGN_MANAGER
--
-- Ends the department's current open manager assignment and
-- inserts the new one. A PL/SQL procedure call is atomic: if
-- any statement fails, everything before it inside the call
-- is rolled back by the server.
--
-- COMMIT/ROLLBACK ownership lesson: the procedure deliberately
-- does NOT commit. The caller (.NET transaction) commits or
-- rolls back as part of its own larger unit of work. A
-- procedure that exits via an unhandled exception undoes its
-- own work and re-raises, so the caller's transaction state
-- is preserved.
-- =========================================================

create or replace procedure sp_assign_manager (
   p_department_id in department_managers.department_id%type,
   p_employee_id   in department_managers.manager_id%type,
   p_start_date    in date
) is
begin
   -- No matching open row is fine: no-op then insert.
   -- chk_mgr_end_date on the OLD row guards p_start_date ordering.
   update department_managers
      set end_date = p_start_date
    where department_id = p_department_id
      and end_date is null;

   insert into department_managers (department_id, manager_id, start_date)
        values (p_department_id, p_employee_id, p_start_date);
end;
/
