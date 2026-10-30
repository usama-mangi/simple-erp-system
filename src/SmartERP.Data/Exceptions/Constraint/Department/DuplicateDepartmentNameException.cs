namespace SmartERP.Data.Exceptions.Constraint.Department;

public class DuplicateDepartmentNameException(Exception innerException)
    : ConstraintException(
        fieldName: "Name",
        fieldException: innerException,
        message: "Department with this name already exists, please try another one."
    )
{ }