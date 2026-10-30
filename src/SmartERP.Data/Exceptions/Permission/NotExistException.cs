namespace SmartERP.Data.Exceptions.Permission;

public class NotExistsException(
    Exception internalException,
    string message = "You are not authorized to perform this action."
) : Exception(message, internalException)
{ }