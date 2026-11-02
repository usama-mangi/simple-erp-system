using System.Security.Claims;
using SmartERP.Data.Abstractions;

namespace SmartERP.Web.Services;

public class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public string? Username => User?.Identity?.Name;
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    public double? UserId
    {
        get
        {
            var v = User?.FindFirstValue("uid");
            return double.TryParse(v, out var d) ? d : null;
        }
    }
    public double? EmployeeId
    {
        get
        {
            var v = User?.FindFirstValue("eid");
            return double.TryParse(v, out var d) ? d : null;
        }
    }
}