namespace SmartERP.Web;

// The only user-facing string for exceptions without a typed message.
// Operators see the cause in server logs; users see exactly this and no raw
// provider text.
public static class UserMessages
{
    public static readonly string GenericFailure =
        "The request could not be completed. Review the values you entered and try again.";
}
