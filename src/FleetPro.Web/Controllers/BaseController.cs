using FleetPro.Data;
using FleetPro.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace FleetPro.Controllers;

/// <summary>
/// Base controller — injects unread alert count into ViewBag for the sidebar badge.
/// All tenant-aware controllers inherit from this.
/// </summary>
public abstract class BaseController : Controller
{
    protected readonly AppDbContext _db;
    protected readonly ICurrentTenantService _current;

    protected BaseController(AppDbContext db, ICurrentTenantService current)
    {
        _db = db;
        _current = current;
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
