namespace SmartERP.Data.Exceptions.Constraint.EmployeeDetail;

public class EndDateOlderThanStartDateException(
    Exception ex
) : ConstraintException(
        fieldName: "end_date",
        fieldException: ex,
        message: "Invalid Data: End Date is set earlier than Start Date."
    )
{ }