using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Exceptions.Constraint;

namespace SmartERP.Data.Exceptions.Constraint.DepartmentManager;

// ORA-00001 against uq_dept_current_manager: this manager already heads a
// department on an open (end_date IS NULL) assignment and may not head a second.
public class DuplicateCurrentManagerException(Exception exception)
    : ConstraintException(
        constraintName: "UQ_DEPT_CURRENT_MANAGER",
        fieldName: "ManagerId",
        fieldException: exception,
        message: "This employee already heads a department."
    )
{ }
