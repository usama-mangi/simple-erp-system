namespace SmartERP.Data.Exceptions.Constraint.EmployeeDetail;

public class EndDateOlderThanStartDateException(
    Exception ex
) : ConstraintException(
        // Covers both check constraints: chk_det_end_date and chk_mgr_end_date
        // share the END_DATE substring the ORA message prints unqualified.
        constraintName: "END_DATE",
        fieldName: "EndDate",
        fieldException: ex,
        message: "Invalid Data: End Date is set earlier than Start Date."
    )
{ }
