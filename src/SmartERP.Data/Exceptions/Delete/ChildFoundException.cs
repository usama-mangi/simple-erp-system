namespace SmartERP.Data.Exceptions.Delete;

public class ChildFoundException(
    Exception internalException,
    string message = "Cannot delete: A record depends on the selected item."
) : Exception(message, internalException)
{ }