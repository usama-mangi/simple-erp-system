namespace SmartERP.Data.Exceptions.Permission;

public class PrivilegeException(
    Exception internalException,
    string message = "You are not allowed to perform this action"
) : Exception(message, internalException)
{ }