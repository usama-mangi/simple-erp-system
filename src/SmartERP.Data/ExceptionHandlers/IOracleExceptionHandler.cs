using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Exceptions.Constraint;

namespace SmartERP.Data.ExceptionHandlers;

public interface IOracleExceptionHandler
{
    public IOracleExceptionHandler HandlePermissionExceptions(OracleException ex);
    public IOracleExceptionHandler HandleConstraintExceptions(OracleException ex, ConstraintException[] cex);
    public IOracleExceptionHandler HandleReadExceptions(OracleException ex);
    public IOracleExceptionHandler HandleCreateExceptions(OracleException ex);
    public IOracleExceptionHandler HandleUpdateExceptions(OracleException ex);
    public IOracleExceptionHandler HandleDeleteExceptions(OracleException ex);
}