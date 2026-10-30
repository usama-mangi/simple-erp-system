namespace SmartERP.Data.Exceptions.Create;

public class InvalidValueException(
    Exception internalException,
    string message = "Provided values are invalid."
) : Exception(message, internalException)
{ }