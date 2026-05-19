using FleetPro.Data;
using FleetPro.Middleware;
using FleetPro.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── SERILOG ──────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/fleetpro-.log", rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ── SERVICES ─────────────────────────────────────────────────
builder.Services.AddLocalization();

builder.Services.AddControllersWithViews(options =>
{
    // Global CSRF protection
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
})
.AddViewLocalization();

// HttpContext accessor (needed before DbContext for audit interceptor)
builder.Services.AddHttpContextAccessor();

// Audit Interceptor
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

// EF Core with Audit Interceptor - suppress migration warning
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(3));
    options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
    // Suppress pending model changes warning - we use SQL script for DB creation
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// Cookie Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();

// Storage Settings (from appsettings.json)
var storageSettings = builder.Configuration.GetSection("Storage").Get<StorageSettings>() ?? new StorageSettings();
builder.Services.AddSingleton(storageSettings);
builder.Services.AddScoped<IStorageService, StorageService>();

// Business Services
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<ITruckService, TruckService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddSingleton<IIdProtector, IdProtector>();

// Response caching & compression
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
});

// ── CONFIGURE ────────────────────────────────────────────────
var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Log.Information("Starting database seeding for: {Database}", db.Database.GetConnectionString());
        await DataSeeder.SeedAsync(db);

        // Verify seed
        var userCount = await db.Users.CountAsync();
        var tenantCount = await db.Tenants.CountAsync();
        Log.Information("Database seeded: {Users} users, {Tenants} tenants", userCount, tenantCount);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database seeding failed: {Message}", ex.Message);
        throw; // Re-throw to see the error on startup
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<FleetPro.Middleware.LanguageMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
