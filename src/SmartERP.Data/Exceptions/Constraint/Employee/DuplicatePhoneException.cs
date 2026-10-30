namespace SmartERP.Data.Exceptions.Constraint.Employee;

public class DuplicatePhoneException(Exception exception)
    : ConstraintException(
        fieldName: "Phone",
        fieldException: exception,
        message: "This phone number is already registered by another employee. Please use another one."
    )
{ }