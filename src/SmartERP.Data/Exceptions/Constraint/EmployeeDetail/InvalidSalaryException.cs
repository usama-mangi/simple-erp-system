namespace SmartERP.Data.Exceptions.Constraint.EmployeeDetail;

public class InvalidSalaryException(
    Exception ex
) : ConstraintException(
        constraintName: "CHK_DET_SALARY",
        fieldName: "Salary",
        fieldException: ex,
        message: "Invalid Data: Salary is set to a negative value."
    )
{ }
