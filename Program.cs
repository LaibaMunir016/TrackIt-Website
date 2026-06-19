using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ProjectWeb.Models;
using ProjectWeb.Data;
using ProjectWeb.Models.Repository;
using Microsoft.AspNetCore.Authorization;
using ProjectWeb.Interface;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------
// 0. PORT BINDING (required for Render)
// ----------------------------------------------------------
// var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
// builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ----------------------------------------------------------
// 1. DATABASE + IDENTITY
// ----------------------------------------------------------

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IIncomeService, IncomeService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddSignalR();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ResetPassword");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/AccessDenied");

    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
});
builder.Services.AddControllersWithViews();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/FinanceTracker/Upgrade";
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("PremiumOnly", p => p.RequireClaim("plan", "premium"));
});

var app = builder.Build();

// ----------------------------------------------------------
// 2. MIDDLEWARE
// ----------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.MapHub<ProjectWeb.Hubs.NotificationHub>("/hubs/notifications").RequireAuthorization();

app.UseAuthentication();
app.UseAuthorization();

// ----------------------------------------------------------
// 3. ENDPOINTS
// ----------------------------------------------------------

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=FinanceTracker}/{action=Home}/{id?}");

app.Run();