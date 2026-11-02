using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartERP.Data.Abstractions;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;
using SmartERP.Web.Services;

namespace SmartERP.Web.Controllers;

[AllowAnonymous]
public class AuthController(
    IAppUserRepository _users,
    IConnectionContext _ctx,
    ILogger<AuthController> _logger
) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.GetByUsernameAsync(model.Username);
        if (user is null || !user.IsActive || !PasswordService.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, user.Username),
            new(System.Security.Claims.ClaimTypes.Role, user.Role),
            new("uid", user.Id.ToString()),
            new("eid", user.EmployeeId.ToString())
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        _ctx.ClientIdentifier = user.Username;
        _logger.LogInformation("User {User} signed in", user.Username);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}