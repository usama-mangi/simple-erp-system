namespace SmartERP.Data.Exceptions.Constraint.Employee;

public class DuplicatePhoneException(Exception exception)
    : ConstraintException(
        constraintName: "UQ_EMPLOYEES_PHONE",
        fieldName: "Phone",
        fieldException: exception,
        message: "This phone number is already registered by another employee. Please use another one."
    )
{ }
