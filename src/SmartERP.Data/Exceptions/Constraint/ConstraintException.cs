namespace SmartERP.Data.Exceptions.Constraint;

// FieldName maps the failure to the ModelState key in MVC.
// ConstraintName is the schema-level name the ORA message prints for a
// violated unique/check/index constraint; matching happens on it, NOT on the
// field name - function-based indexes report synthetic columns (SYS_NCxxxx$),
// so a field-text match misses exactly when it matters most.
public class ConstraintException(
    string constraintName,
    string fieldName,
    Exception fieldException,
    string message
    ) : Exception(message, fieldException)
{
    public string FieldName { get; } = fieldName;
    public string ConstraintName { get; } = constraintName;
    public Exception FieldException { get; } = fieldException;
}
