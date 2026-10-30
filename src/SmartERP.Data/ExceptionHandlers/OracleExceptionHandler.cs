using Oracle.ManagedDataAccess.Client;
using SmartERP.Data.Exceptions;
using SmartERP.Data.Exceptions.Create;
using SmartERP.Data.Exceptions.Delete;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Permission;
using SmartERP.Data.Exceptions.Read;
using SmartERP.Data.Exceptions.Update;

namespace SmartERP.Data.ExceptionHandlers;

public class OracleExceptionHandler()
{
    public OracleExceptionHandler HandlePermissionExceptions(OracleException ex)
    {
        return ex.Number switch
        {
            942 => throw new NotExistsException(ex),
            1031 => throw new PrivilegeException(ex),
            _ => this,
        };
    }

    public OracleExceptionHandler HandleConstraintExceptions(OracleException ex, ConstraintException[] constraintException)
    {
        foreach (var constraint in constraintException)
            if (ex.Message.ToLower().Contains(constraint.FieldName.ToLower())) throw constraint.FieldException;

        return this;
    }

    public OracleExceptionHandler HandleReadExceptions(OracleException ex)
    {
        return ex.Number switch
        {
            1403 => throw new NullValueException(ex),
            1427 => throw new TooManyResultsException(ex),
            _ => this,
        };
    }

    public OracleExceptionHandler HandleCreateExceptions(OracleException ex)
    {
        return ex.Number switch
        {
            2291 => throw new ForeignKeyException(ex),
            1400 => throw new NullValueException(ex),
            12899 => throw new InvalidValueException(ex),
            _ => this,
        };
    }

    public OracleExceptionHandler HandleUpdateExceptions(OracleException ex)
    {
        return ex.Number switch
        {
            2291 => throw new ParentNotFoundException(ex),
            1407 => throw new InvalidValueException(ex),
            _ => this,
        };
    }

    public OracleExceptionHandler HandleDeleteExceptions(OracleException ex)
    {
        return ex.Number switch
        {
            2292 => throw new ChildFoundException(ex),
            _ => this,
        };
    }
}