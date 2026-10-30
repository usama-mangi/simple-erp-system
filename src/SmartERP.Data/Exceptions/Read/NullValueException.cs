namespace SmartERP.Data.Exceptions;

public class NullValueException(
    Exception internalException,
    string message = "A required value was not provided"
) : Exception(message, internalException)
{ }