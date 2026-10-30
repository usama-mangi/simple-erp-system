namespace SmartERP.Data.Exceptions.Create;

public class ForeignKeyException(
    Exception internalException,
    string message = "The parent record does not exist."
) : Exception(message, internalException)
{ }