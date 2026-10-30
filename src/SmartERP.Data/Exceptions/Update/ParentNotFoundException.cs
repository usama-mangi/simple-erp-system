namespace SmartERP.Data.Exceptions.Update;

public class ParentNotFoundException(
    Exception internalException,
    string message = "Parent record not found."
) : Exception(message, internalException)
{ }