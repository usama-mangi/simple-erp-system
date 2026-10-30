namespace SmartERP.Data.Exceptions.Read;

public class TooManyResultsException(
    Exception internalException,
    string message = "Got more than expected results."
) : Exception(message, internalException)
{ }