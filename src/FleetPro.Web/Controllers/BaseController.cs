using FleetPro.Data;
using FleetPro.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace FleetPro.Controllers;

public abstract class BaseController : Controller
{
    protected readonly AppDbContext _db;
    protected readonly ICurrentTenantService _current;
    protected readonly IIdProtector _ids;

    protected BaseController(AppDbContext db, ICurrentTenantService current)
    {
        _db = db;
        _current = current;
        // Resolved lazily via HttpContext so we don't need to change every constructor signature
        _ids = null!;
    }

    // Encrypt an int ID for use in route/query parameters
    protected string EId(int id) => (_ids ?? HttpContext.RequestServices.GetRequiredService<IIdProtector>()).Protect(id);

    // Decrypt an encrypted token back to int — returns 0 on failure
    protected int DId(string token)
    {
        try { return HttpContext.RequestServices.GetRequiredService<IIdProtector>().Unprotect(token); }
        catch { return 0; }
    }

    // Called before every action — sets language from cookie
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        // Read from middleware-set value first, fallback to cookie
        var lang = context.HttpContext.Items["Language"]?.ToString();
        if (string.IsNullOrEmpty(lang))
            context.HttpContext.Request.Cookies.TryGetValue("FleetPro_Lang", out lang);

        ViewData["Lang"] = lang == "hi" ? "hi" : "en";
    }

    // Returns null if allowed, or a redirect with a modal message if the user lacks the permission.
    protected IActionResult? CheckPermission(string permissionKey)
    {
        if (_current.HasPermission(permissionKey)) return null;

        // Format "trucks.create" → "Create Trucks"
        var parts = permissionKey.Split('.');
        var action   = parts.Length > 1 ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[1]) : permissionKey;
        var resource = parts.Length > 0 ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[0]) : "";

        TempData["AccessDenied"]            = "You do not have permission to perform this action.";
        TempData["AccessDeniedAction"]      = action;
        TempData["AccessDeniedResource"]    = resource;

        return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } referer ? referer : "/Dashboard");
    }

    // Called after every action — safe to use async here
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        if (User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                var count = _current.IsSuperAdmin
                    ? _db.Alerts.Count(a => !a.IsRead && !a.IsDeleted)
                    : _db.Alerts.Count(a => a.TenantId == _current.TenantId && !a.IsRead && !a.IsDeleted);

                ViewBag.AlertCount = count;
            }
            catch
            {
                ViewBag.AlertCount = 0;
            }
        }
    }
}
