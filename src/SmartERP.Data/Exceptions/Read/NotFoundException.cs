namespace SmartERP.Data.Exceptions.Read;

public class NotFoundException(
    string entityType,
    double id,
    Exception? internalException = null
) : Exception($"{entityType} record with id {id} was not found.", internalException)
{ }
