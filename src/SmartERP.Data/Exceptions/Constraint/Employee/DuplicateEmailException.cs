namespace SmartERP.Data.Exceptions.Constraint.Employee;

public class DuplicateEmailException(Exception exception)
    : ConstraintException(
        fieldName: "Email",
        fieldException: exception,
        message: "Employee with this email address already exists. Try another one."
    )
{ }