namespace SmartERP.Data.Exceptions.Constraint.Department;

// ORA-02290 against chk_dept_parent_not_self: the row was pointed at itself.
// The update dropdown never offers the department itself, so this is the
// database safety net for a forged or concurrent write.
public class DepartmentParentSelfException(
    Exception innerException
) : ConstraintException(
        constraintName: "CHK_DEPT_PARENT_NOT_SELF",
        fieldName: "ParentDepartmentId",
        fieldException: innerException,
        message: "A department cannot report to itself."
    )
{ }
