namespace SmartERP.Data.Exceptions.Constraint.Employee;

public class DuplicateEmailException(Exception exception)
    : ConstraintException(
        constraintName: "UQ_EMPLOYEES_EMAIL",
        fieldName: "Email",
        fieldException: exception,
        message: "Employee with this email address already exists. Try another one."
    )
{ }