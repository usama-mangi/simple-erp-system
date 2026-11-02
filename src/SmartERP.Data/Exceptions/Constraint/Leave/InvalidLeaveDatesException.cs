namespace SmartERP.Data.Exceptions.Constraint.Leave;

// ORA-02290 against chk_lr_dates: end_date landed before start_date, which the
// view model normally catches first — this is the database's safety net.
public class InvalidLeaveDatesException(
    Exception ex
) : ConstraintException(
        constraintName: "CHK_LR_DATES",
        fieldName: "EndDate",
        fieldException: ex,
        message: "The end date cannot be earlier than the start date."
    )
{ }
