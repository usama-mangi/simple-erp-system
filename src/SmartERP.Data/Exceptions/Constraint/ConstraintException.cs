namespace SmartERP.Data.Exceptions.Constraint;

public class ConstraintException(
    string fieldName,
    Exception fieldException,
    string message
    ) : Exception(message, fieldException)
{
    public string FieldName { get; } = fieldName;
    public Exception FieldException { get; } = fieldException;
}