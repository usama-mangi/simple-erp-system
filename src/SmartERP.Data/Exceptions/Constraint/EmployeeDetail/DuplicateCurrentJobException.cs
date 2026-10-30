using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Exceptions.Constraint;

namespace SmartERP.Data.Exceptions.Constraint.EmployeeDetail;

// ORA-00001 against uq_det_current_job: an employee already has exactly one
// open (end_date IS NULL) employment_details row and may not open another.
public class DuplicateCurrentJobException(Exception exception)
    : ConstraintException(
        constraintName: "UQ_DET_CURRENT_JOB",
        fieldName: "StartDate",
        fieldException: exception,
        message: "This employee already has an open job assignment."
    )
{ }
