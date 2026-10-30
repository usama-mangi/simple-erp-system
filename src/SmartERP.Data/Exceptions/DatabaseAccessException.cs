using SmartERP.Data.Exceptions;

namespace SmartERP.Data.Exceptions;

// Thrown by repositories for every unmapped OracleException after the typed
// chain. Message is deliberately generic: the web layer renders it as-is, so
// raw ORA text never builds a user-facing string.
public class DatabaseAccessException(Exception internalException)
    : Exception("The request could not be completed. Review the values you entered and try again.", internalException)
{ }
