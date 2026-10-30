namespace SmartERP.Data.Exceptions.Constraint.Department;

public class DuplicateDepartmentCodeException(Exception innerException)
    : ConstraintException(
        constraintName: "UQ_DEPARTMENTS_CODE",
        fieldName: "Code",
        fieldException: innerException,
        message: "Code already in use, please try another one."
    )
{ }
