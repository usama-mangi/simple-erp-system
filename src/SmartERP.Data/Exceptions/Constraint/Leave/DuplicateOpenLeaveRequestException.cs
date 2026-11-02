using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Exceptions.Constraint;

namespace SmartERP.Data.Exceptions.Constraint.Leave;

// ORA-00001 against uq_lr_open_request: an employee may have exactly one
// Pending leave request at a time, so a second one has to be decided first.
public class DuplicateOpenLeaveRequestException(Exception exception)
    : ConstraintException(
        constraintName: "UQ_LR_OPEN_REQUEST",
        fieldName: "StartDate",
        fieldException: exception,
        message: "This employee already has a leave request waiting for a decision."
    )
{ }
