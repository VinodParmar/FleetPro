using System.Net;
using Newtonsoft.Json;

namespace FleetPro.Middleware;

// ═══════════════════════════════════════════════════
//  GLOBAL EXCEPTION MIDDLEWARE
// ═══════════════════════════════════════════════════
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next; _logger = logger; _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message} | Path: {Path}",
                ex.Message, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = ex switch
        {
            UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                error = _env.IsDevelopment() ? ex.Message : "An error occurred.",
                detail = _env.IsDevelopment() ? ex.StackTrace : null
            };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
        else
        {
            context.Response.Redirect($"/Error?code={context.Response.StatusCode}");
        }
    }
}

// ═══════════════════════════════════════════════════
//  TENANT MIDDLEWARE — validates tenant context
// ═══════════════════════════════════════════════════
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip for static files, login, error pages
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/account") || path.StartsWith("/error") ||
            path.StartsWith("/lib") || path.StartsWith("/css") ||
            path.StartsWith("/js") || path.StartsWith("/uploads"))
        {
            await _next(context);
            return;
        }

        // User is authenticated - extract tenant from claims (already done via CurrentTenantService)
        await _next(context);
    }
}

// ═══════════════════════════════════════════════════
//  LANGUAGE MIDDLEWARE — handles language switching
// ═══════════════════════════════════════════════════
public class LanguageMiddleware
{
    private readonly RequestDelegate _next;
    private const string LANG_COOKIE = "FleetPro_Lang";

    public LanguageMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for lang query parameter
        if (context.Request.Query.TryGetValue("lang", out var langValue))
        {
            var lang = langValue.ToString().ToLower();
            if (lang == "hi" || lang == "en")
            {
                // Set cookie for 1 year
                context.Response.Cookies.Append(LANG_COOKIE, lang,
                    new Microsoft.AspNetCore.Http.CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        HttpOnly = true,
                        Secure = true,
                        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
                    });

                // Redirect to remove the lang query parameter
                var qs = context.Request.QueryString.Value ?? "";
                qs = qs.Replace($"?lang={lang}&", "?")
                       .Replace($"?lang={lang}", "")
                       .Replace($"&lang={lang}", "");
                var redirectUrl = context.Request.Path + qs;
                context.Response.Redirect(redirectUrl);
                return;
            }
        }

        // Get language from cookie or default to 'en'
        var currentLang = "en";
        if (context.Request.Cookies.TryGetValue(LANG_COOKIE, out var cookieLang))
        {
            currentLang = cookieLang == "hi" ? "hi" : "en";
        }

        // Store in HttpContext for use in controllers/views
        context.Items["Language"] = currentLang;

        await _next(context);
    }
}


// ═══════════════════════════════════════════════════
//  PERMISSION ATTRIBUTE
// ═══════════════════════════════════════════════════
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute
{
    public string Permission { get; }
    public RequirePermissionAttribute(string permission) => Permission = permission;
}

// ═══════════════════════════════════════════════════
//  PERMISSION FILTER
// ═══════════════════════════════════════════════════
public class PermissionFilter : Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter
{
    private readonly FleetPro.Services.ICurrentTenantService _current;
    private readonly string _permission;

    public PermissionFilter(FleetPro.Services.ICurrentTenantService current, string permission)
    {
        _current = current;
        _permission = permission;
    }

    public void OnAuthorization(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (!_current.HasPermission(_permission))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
        }
    }
}
