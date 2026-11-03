using Microsoft.AspNetCore.Authentication.Cookies;
using SmartERP.Data.Abstractions;
using SmartERP.Data.ExceptionHandlers;
using SmartERP.Data.Infrastructure;
using SmartERP.Data.Repositories;
using SmartERP.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IConnectionContext, ConnectionContext>();
builder.Services.AddSingleton<IDbConnectionFactory, OracleDbConnection>();
builder.Services.AddSingleton<IOracleExceptionHandler, OracleExceptionHandler>();

builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmploymentRepository, EmploymentRepository>();
builder.Services.AddScoped<IDepartmentManagerRepository, DepartmentManagerRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Auth/Login";
        o.LogoutPath = "/Auth/Logout";
        o.AccessDeniedPath = "/Auth/Login";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Propagate the signed-in user to the connection context for audit triggers.
app.Use(async (ctx, next) =>
{
    var cu = ctx.RequestServices.GetRequiredService<ICurrentUser>();
    var cc = ctx.RequestServices.GetRequiredService<IConnectionContext>();
    if (cu.IsAuthenticated && !string.IsNullOrWhiteSpace(cu.Username))
    {
        cc.ClientIdentifier = cu.Username;
    }
    await next();
});

app.UseStaticFiles();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}")
    .WithStaticAssets();

app.Run();