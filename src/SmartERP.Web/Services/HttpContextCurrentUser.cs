using System.Globalization;
using System.Security.Claims;
using SmartERP.Data.Abstractions;

namespace SmartERP.Web.Services;

public class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public string? Username => User?.Identity?.Name;
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    public decimal? UserId
    {
        get
        {
            var v = User?.FindFirstValue("uid");
            return decimal.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) ? d : null;
        }
    }
    public decimal? EmployeeId
    {
        get
        {
            var v = User?.FindFirstValue("eid");
            return decimal.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) ? d : null;
        }
    }
}