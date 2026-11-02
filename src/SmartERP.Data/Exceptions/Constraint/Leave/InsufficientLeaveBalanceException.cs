namespace SmartERP.Data.Exceptions.Constraint.Leave;

// ORA-02290 against chk_bal_used: used_days tried to climb above the
// entitlement (or below zero). The approve path checks the remaining days
// before it writes, so reaching this means the balance moved underneath it.
public class InsufficientLeaveBalanceException(
    Exception ex
) : ConstraintException(
        constraintName: "CHK_BAL_USED",
        fieldName: "EntitlementDays",
        fieldException: ex,
        message: "The balance does not hold enough remaining days for this."
    )
{ }
