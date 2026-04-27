using System.Security.Claims;
using FleetPro.Data;
using FleetPro.Models.Entities;
using FleetPro.Models.ViewModels;
using FleetPro.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetPro.Controllers;

// ROLE
[Authorize(Roles="SuperAdmin")] public class RoleController : BaseController {
    public RoleController(AppDbContext db, ICurrentTenantService c) : base(db, c) {}

    public async Task<IActionResult> Index() {
        var allRolePerms = await _db.RolePermissions.IgnoreQueryFilters().ToListAsync();
        var allUserRoles = await _db.UserRoles.ToListAsync();
        var roles = await _db.Roles
            .Where(r => !r.IsDeleted && (_current.IsSuperAdmin || r.Name != "SuperAdmin"))
            .OrderBy(r => r.Name).ToListAsync();
        var vm = roles.Select(r => new RoleListViewModel {
            Id              = r.Id,
            Name            = r.Name,
            Description     = r.Description,
            IsSystemRole    = r.IsSystemRole,
            PermissionCount = allRolePerms.Count(rp => rp.RoleId == r.Id && rp.IsGranted),
            UserCount       = allUserRoles.Count(ur => ur.RoleId == r.Id)
        }).ToList();

        // Pass full permission matrix for the overview table
        var allPermissions = await _db.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Action).ToListAsync();
        ViewBag.AllPermissions = allPermissions;
        ViewBag.GrantedMatrix  = allRolePerms
            .Where(rp => rp.IsGranted)
            .GroupBy(rp => (rp.RoleId, rp.PermissionId))
            .ToDictionary(g => g.Key, _ => true);

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReseedPermissions() {
        await DataSeeder.SeedRolePermissionsAsync(_db);
        TempData["Success"] = "Role permissions re-seeded successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Permissions(string eid) {
        var id = DId(eid); if (id == 0) return NotFound();
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();
        var all = await _db.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Action).ToListAsync();
        var allRolePerms = await _db.RolePermissions.IgnoreQueryFilters().ToListAsync();
        var granted = allRolePerms.Where(rp => rp.RoleId == id && rp.IsGranted).Select(rp => rp.PermissionId).ToHashSet();
        return View(new RolePermissionViewModel {
            RoleId      = role.Id,
            RoleName    = role.Name,
            Description = role.Description,
            IsSystemRole= role.IsSystemRole,
            Groups = all.GroupBy(p => p.Module).Select(g => new PermissionGroupViewModel {
                Module = g.Key,
                Permissions = g.Select(p => new PermissionItemViewModel {
                    PermissionId   = p.Id,
                    Key            = p.Key,
                    Action         = p.Action,
                    IsGrantedByRole= granted.Contains(p.Id)
                }).ToList()
            }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Permissions(string eid, List<int> grantedPermissions) {
        var id = DId(eid); if (id == 0) return NotFound();
        var role = await _db.Roles.FindAsync(id); if (role == null) return NotFound();
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);
        var all = await _db.Permissions.Select(p => p.Id).ToListAsync();
        foreach (var permId in all)
            _db.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = permId, IsGranted = grantedPermissions.Contains(permId) });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Permissions updated for role '{role.Name}'.";
        return RedirectToAction(nameof(Permissions), new { eid = EId(id) });
    }
}

// ACCOUNT
public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly ILogger<AccountController> _log;
    public AccountController(IAuthService auth, ILogger<AccountController> log) { _auth=auth; _log=log; }

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        var lang = context.HttpContext.Items["Language"]?.ToString();
        if (string.IsNullOrEmpty(lang))
            context.HttpContext.Request.Cookies.TryGetValue("FleetPro_Lang", out lang);
        lang = lang == "hi" ? "hi" : "en";
        ViewData["Lang"] = lang;
        _log.LogInformation($"AccountController.OnActionExecuting: Set ViewData[Lang]={lang}");
        base.OnActionExecuting(context);
    }

    [HttpGet] public IActionResult Login(string? returnUrl=null) {
        if (User.Identity?.IsAuthenticated==true) return RedirectToAction("Index","Dashboard");
        return View(new LoginViewModel { ReturnUrl=returnUrl });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model) {
        if (!ModelState.IsValid) return View(model);
        var (ok,user,err) = await _auth.LoginAsync(model.Email, model.Password);
        if (!ok) { ModelState.AddModelError("",err); return View(model); }
        var claims = await _auth.BuildClaimsAsync(user!);
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var props = new AuthenticationProperties { IsPersistent=model.RememberMe,
            ExpiresUtc=model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), props);
        _log.LogInformation("User {Email} logged in",model.Email);
        return LocalRedirect(model.ReturnUrl ?? "/Dashboard");
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Unlock([FromForm] string password) {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var (ok, _, _) = await _auth.LoginAsync(email!, password);
        return Json(new { success = ok });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout() {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
    [Authorize] public IActionResult AccessDenied() => View();
}

// DASHBOARD
[Authorize] public class DashboardController : BaseController {
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc, AppDbContext db, ICurrentTenantService c) : base(db,c) => _svc=svc;
    public async Task<IActionResult> Index() => View(await _svc.GetStatsAsync());
}

// TENANT
[Authorize(Roles="SuperAdmin")] public class TenantController : BaseController {
    private readonly ITenantService _svc;
    public TenantController(ITenantService svc, AppDbContext db, ICurrentTenantService c) : base(db,c) => _svc=svc;

    public async Task<IActionResult> Index() {
        var tenants = await _svc.GetAllAsync();
        var stats = await _db.Tenants.Select(t=>new{t.Id,
            TC=t.Trucks.Count(x=>!x.IsDeleted), DC=t.Drivers.Count(x=>!x.IsDeleted), UC=t.Users.Count(x=>!x.IsDeleted)
        }).ToListAsync();

        // Get TenantAdmin (owner) for each tenant
        var adminRoleId = await _db.Roles.Where(r => r.Name == "TenantAdmin").Select(r => r.Id).FirstOrDefaultAsync();
        var owners = await _db.Users
            .Where(u => !u.IsDeleted && u.TenantId.HasValue && u.UserRoles.Any(ur => ur.RoleId == adminRoleId))
            .Select(u => new { u.TenantId, u.FullName, u.Email })
            .ToListAsync();

        return View(tenants.Select(t=>new TenantViewModel{
            Id=t.Id,CompanyName=t.CompanyName,Subdomain=t.Subdomain,ContactPerson=t.ContactPerson,
            Email=t.Email,Phone=t.Phone,Plan=t.Plan,Status=t.Status,City=t.City,State=t.State,
            GstNumber=t.GstNumber,MaxTrucks=t.MaxTrucks,MaxUsers=t.MaxUsers,
            TruckCount=stats.FirstOrDefault(s=>s.Id==t.Id)?.TC??0,
            DriverCount=stats.FirstOrDefault(s=>s.Id==t.Id)?.DC??0,
            UserCount=stats.FirstOrDefault(s=>s.Id==t.Id)?.UC??0,
            OwnerName=owners.FirstOrDefault(o=>o.TenantId==t.Id)?.FullName??"",
            OwnerEmail=owners.FirstOrDefault(o=>o.TenantId==t.Id)?.Email??""
        }).ToList());
    }
    public IActionResult Create() => View(new TenantViewModel{MaxTrucks=10,MaxUsers=5});

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenantViewModel vm) {
        if (!ModelState.IsValid) return View(vm);
        if (await _db.Tenants.AnyAsync(t=>t.Subdomain==vm.Subdomain))
        { ModelState.AddModelError("Subdomain","Subdomain taken."); return View(vm); }
        if (await _db.Users.AnyAsync(u=>u.Email==vm.OwnerEmail))
        { ModelState.AddModelError("OwnerEmail","Email already registered."); return View(vm); }
        var tenant = new Tenant{CompanyName=vm.CompanyName,Subdomain=vm.Subdomain,ContactPerson=vm.ContactPerson,
            Email=vm.Email,Phone=vm.Phone,GstNumber=vm.GstNumber,Address=vm.Address,City=vm.City,State=vm.State,
            Plan=vm.Plan,Status=vm.Status,MaxTrucks=vm.MaxTrucks,MaxUsers=vm.MaxUsers,
            SubscriptionStartDate=DateTime.UtcNow,SubscriptionEndDate=DateTime.UtcNow.AddMonths(vm.Plan==TenantPlan.Starter?1:12)};
        var owner = new ApplicationUser{FullName=vm.OwnerName,Email=vm.OwnerEmail,Phone=vm.OwnerPhone,PasswordHash=vm.OwnerPassword};
        await _svc.CreateAsync(tenant,owner);
        TempData["Success"]=$"Tenant '{tenant.CompanyName}' created!";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(string eid) {
        var id=DId(eid); if(id==0) return NotFound();
        var t=await _svc.GetByIdAsync(id); if(t==null) return NotFound();
        return View("Create",new TenantViewModel{Id=t.Id,CompanyName=t.CompanyName,Subdomain=t.Subdomain,
            ContactPerson=t.ContactPerson,Email=t.Email,Phone=t.Phone,GstNumber=t.GstNumber,
            Address=t.Address,City=t.City,State=t.State,Plan=t.Plan,Status=t.Status,
            MaxTrucks=t.MaxTrucks,MaxUsers=t.MaxUsers,OwnerName="",OwnerEmail="",OwnerPassword="placeholder"});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string eid, TenantViewModel vm) {
        var id=DId(eid); if(id==0) return NotFound();
        ModelState.Remove("OwnerName"); ModelState.Remove("OwnerEmail"); ModelState.Remove("OwnerPassword");
        if (!ModelState.IsValid) return View("Create",vm);
        var t=await _svc.GetByIdAsync(id); if(t==null) return NotFound();
        t.CompanyName=vm.CompanyName;t.ContactPerson=vm.ContactPerson;t.Email=vm.Email;t.Phone=vm.Phone;
        t.GstNumber=vm.GstNumber;t.Address=vm.Address;t.City=vm.City;t.State=vm.State;
        t.Plan=vm.Plan;t.Status=vm.Status;t.MaxTrucks=vm.MaxTrucks;t.MaxUsers=vm.MaxUsers;
        await _svc.UpdateAsync(t); TempData["Success"]="Tenant updated.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string eid) {
        var id=DId(eid); if(id==0) return NotFound();
        await _svc.DeleteAsync(id); TempData["Success"]="Tenant deleted.";
        return RedirectToAction(nameof(Index));
    }
}

// USER
[Authorize] public class UserController : BaseController {
    private readonly IUserService _svc;
    public UserController(IUserService svc, AppDbContext db, ICurrentTenantService c) : base(db,c) => _svc=svc;

    public async Task<IActionResult> Index() =>
        View(await _svc.GetUsersAsync(_current.IsSuperAdmin ? null : _current.TenantId));

    public async Task<IActionResult> Create() {
        if (CheckPermission("users.create") is { } r) return r;
        await Dropdowns(); return View(new UserViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserViewModel vm) {
        if (CheckPermission("users.create") is { } r) return r;
        if (!ModelState.IsValid) { await Dropdowns(); return View(vm); }
        if (await _db.Users.AnyAsync(u=>u.Email==vm.Email))
        { ModelState.AddModelError("Email","Email already registered."); await Dropdowns(); return View(vm); }

        // TenantAdmin can only create users for their own tenant
        var targetTenantId = _current.IsSuperAdmin ? vm.TenantId : _current.TenantId;

        await _svc.CreateAsync(new ApplicationUser{FullName=vm.FullName,Email=vm.Email,Phone=vm.Phone,
            TenantId=targetTenantId,Status=vm.Status,PasswordHash=vm.Password!}, vm.RoleId);
        TempData["Success"]="User created!"; return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string eid) {
        if (CheckPermission("users.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        var u=await _svc.GetByIdAsync(id); if(u==null) return NotFound();
        await Dropdowns();
        return View("Create",new UserViewModel{Id=u.Id,FullName=u.FullName,Email=u.Email,Phone=u.Phone,
            TenantId=u.TenantId,Status=u.Status,RoleId=u.UserRoles.FirstOrDefault()?.RoleId??0,
            CurrentRole=u.UserRoles.FirstOrDefault()?.Role?.Name});
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string eid, UserViewModel vm) {
        if (CheckPermission("users.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        ModelState.Remove("Password");
        if (!ModelState.IsValid) { await Dropdowns(); return View("Create",vm); }
        var u=await _svc.GetByIdAsync(id); if(u==null) return NotFound();
        u.FullName=vm.FullName;u.Phone=vm.Phone;u.Status=vm.Status;
        if (!string.IsNullOrWhiteSpace(vm.Password))
            u.PasswordHash=BCrypt.Net.BCrypt.HashPassword(vm.Password);
        var er=await _db.UserRoles.FirstOrDefaultAsync(ur=>ur.UserId==id);
        if (er!=null&&er.RoleId!=vm.RoleId){er.RoleId=vm.RoleId;await _db.SaveChangesAsync();}
        await _svc.UpdateAsync(u); TempData["Success"]="User updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string eid) {
        if (CheckPermission("users.delete") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        await _svc.DeleteAsync(id); TempData["Success"]="User deleted.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles="SuperAdmin,TenantAdmin")]
    public async Task<IActionResult> Permissions(string eid) {
        var id=DId(eid); if(id==0) return NotFound();
        var u=await _svc.GetByIdAsync(id); if(u==null) return NotFound();
        var all=await _db.Permissions.ToListAsync();
        var roleIds=u.UserRoles.Select(ur=>ur.RoleId).ToList();

        // Materialize RolePermissions to avoid CTE generation
        var allRolePermissions = await _db.RolePermissions.IgnoreQueryFilters().ToListAsync();
        var rp = allRolePermissions
            .Where(x => roleIds.Contains(x.RoleId) && x.IsGranted)
            .Select(x => x.PermissionId)
            .ToList();

        var up=await _svc.GetUserPermissionsAsync(id);
        return View(new UserPermissionViewModel{UserId=id,UserName=u.FullName,
            Groups=all.GroupBy(p=>p.Module).Select(g=>new PermissionGroupViewModel{Module=g.Key,
                Permissions=g.Select(p=>new PermissionItemViewModel{PermissionId=p.Id,Key=p.Key,Action=p.Action,
                    IsGrantedByRole=rp.Contains(p.Id),
                    IsOverriddenByUser=up.FirstOrDefault(x=>x.PermissionId==p.Id)?.IsGranted}).ToList()}).ToList()});
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles="SuperAdmin,TenantAdmin")]
    public async Task<IActionResult> Permissions(string eid, Dictionary<string,string> permissionValues) {
        var id=DId(eid); if(id==0) return NotFound();
        var u = await _svc.GetByIdAsync(id); if(u==null) return NotFound();
        var overrides=permissionValues.Where(kv=>kv.Key.StartsWith("perm_")&&kv.Value!="inherit")
            .ToDictionary(kv=>int.Parse(kv.Key.Replace("perm_","")),kv=>kv.Value=="grant");
        await _svc.UpdatePermissionsAsync(id,overrides);
        TempData["Success"]="Permissions updated."; return RedirectToAction(nameof(Permissions),new{eid=EId(id)});
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles="SuperAdmin,TenantAdmin")]
    public async Task<IActionResult> ResetPassword(string eid, string NewPassword, string ConfirmPassword) {
        var id=DId(eid); if(id==0) return NotFound();
        if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 8) {
            TempData["Error"]="Password must be at least 8 characters.";
            return RedirectToAction(nameof(Index));
        }
        if (NewPassword != ConfirmPassword) {
            TempData["Error"]="Passwords do not match.";
            return RedirectToAction(nameof(Index));
        }
        var u=await _svc.GetByIdAsync(id); if(u==null) return NotFound();
        u.PasswordHash=BCrypt.Net.BCrypt.HashPassword(NewPassword);
        await _svc.UpdateAsync(u);
        TempData["Success"]=$"Password reset for {u.FullName}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task Dropdowns() {
        // SuperAdmin sees all roles; TenantAdmin sees all except SuperAdmin
        var rolesQuery = _db.Roles.Where(r => !r.IsDeleted);
        if (!_current.IsSuperAdmin)
            rolesQuery = rolesQuery.Where(r => r.Name != "SuperAdmin" && r.Name != "TenantAdmin");
        ViewBag.Roles = new SelectList(await rolesQuery.ToListAsync(), "Id", "Name");

        // Only SuperAdmin sees the tenant picker
        if (_current.IsSuperAdmin)
            ViewBag.Tenants = new SelectList(await _db.Tenants.OrderBy(t=>t.CompanyName).ToListAsync(), "Id", "CompanyName");
    }
}

// TRUCK
[Authorize] public class TruckController : BaseController {
    private readonly ITruckService _svc;
    public TruckController(ITruckService svc, AppDbContext db, ICurrentTenantService c) : base(db,c) => _svc=svc;

    public async Task<IActionResult> Index(TruckStatus? status, string? search) {
        var trucks=await _svc.GetAllAsync();
        if (status.HasValue) trucks=trucks.Where(t=>t.Status==status).ToList();
        if (!string.IsNullOrWhiteSpace(search)) trucks=trucks.Where(t=>
            t.NumberPlate.Contains(search,StringComparison.OrdinalIgnoreCase)||
            t.Model.Contains(search,StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.StatusFilter=status; ViewBag.Search=search; return View(trucks);
    }
    public async Task<IActionResult> Create() {
        if (CheckPermission("trucks.create") is { } r) return r;
        await PopT(); return View(new TruckViewModel());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TruckViewModel vm) {
        if (CheckPermission("trucks.create") is { } r) return r;
        if (!ModelState.IsValid) { await PopT(); return View(vm); }
        await _svc.CreateAsync(ToEntity(vm)); TempData["Success"]=$"Truck {vm.NumberPlate} added.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(string eid) {
        if (CheckPermission("trucks.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        var t=await _svc.GetByIdAsync(id); if(t==null) return NotFound();
        await PopT(); return View("Create",ToVM(t));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string eid, TruckViewModel vm) {
        if (CheckPermission("trucks.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        if (!ModelState.IsValid) { await PopT(); return View("Create",vm); }
        var t=await _svc.GetByIdAsync(id); if(t==null) return NotFound();
        t.NumberPlate=vm.NumberPlate;t.Model=vm.Model;t.Make=vm.Make;t.ManufacturingYear=vm.ManufacturingYear;
        t.EngineNumber=vm.EngineNumber;t.ChassisNumber=vm.ChassisNumber;t.FitnessExpiry=vm.FitnessExpiry;
        t.InsuranceExpiry=vm.InsuranceExpiry;t.TaxExpiry=vm.TaxExpiry;t.PermitExpiry=vm.PermitExpiry;
        t.InsurancePolicyNumber=vm.InsurancePolicyNumber;t.Status=vm.Status;
        t.LoadCapacityTons=vm.LoadCapacityTons;t.Notes=vm.Notes;
        await _svc.UpdateAsync(t); TempData["Success"]="Truck updated.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string eid) {
        if (CheckPermission("trucks.delete") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        await _svc.DeleteAsync(id); TempData["Success"]="Truck deleted.";
        return RedirectToAction(nameof(Index));
    }
    private Truck ToEntity(TruckViewModel vm) => new(){
        NumberPlate=vm.NumberPlate,Model=vm.Model,Make=vm.Make,ManufacturingYear=vm.ManufacturingYear,
        EngineNumber=vm.EngineNumber,ChassisNumber=vm.ChassisNumber,FitnessExpiry=vm.FitnessExpiry,
        InsuranceExpiry=vm.InsuranceExpiry,TaxExpiry=vm.TaxExpiry,PermitExpiry=vm.PermitExpiry,
        InsurancePolicyNumber=vm.InsurancePolicyNumber,Status=vm.Status,LoadCapacityTons=vm.LoadCapacityTons,
        Notes=vm.Notes,TenantId=_current.IsSuperAdmin?(vm.TenantId??0):_current.TenantId!.Value};
    private static TruckViewModel ToVM(Truck t) => new(){Id=t.Id,NumberPlate=t.NumberPlate,Model=t.Model,
        Make=t.Make,ManufacturingYear=t.ManufacturingYear,EngineNumber=t.EngineNumber,ChassisNumber=t.ChassisNumber,
        FitnessExpiry=t.FitnessExpiry,InsuranceExpiry=t.InsuranceExpiry,TaxExpiry=t.TaxExpiry,
        PermitExpiry=t.PermitExpiry,InsurancePolicyNumber=t.InsurancePolicyNumber,Status=t.Status,
        LoadCapacityTons=t.LoadCapacityTons,Notes=t.Notes,TenantId=t.TenantId,TenantName=t.Tenant?.CompanyName};
    private async Task PopT() { if(_current.IsSuperAdmin) ViewBag.Tenants=new SelectList(await _db.Tenants.OrderBy(t=>t.CompanyName).ToListAsync(),"Id","CompanyName"); }
}

// DRIVER
[Authorize] public class DriverController : BaseController {
    private readonly IDriverService _svc;
    public DriverController(IDriverService svc, AppDbContext db, ICurrentTenantService c) : base(db,c) => _svc=svc;

    public async Task<IActionResult> Index(DriverStatus? status, string? search) {
        var drivers=await _svc.GetAllAsync();
        if (status.HasValue) drivers=drivers.Where(d=>d.Status==status).ToList();
        if (!string.IsNullOrWhiteSpace(search)) drivers=drivers.Where(d=>
            d.FullName.Contains(search,StringComparison.OrdinalIgnoreCase)||
            (d.LicenseNumber?.Contains(search,StringComparison.OrdinalIgnoreCase)??false)).ToList();
        return View(drivers);
    }
    public async Task<IActionResult> Create() {
        if (CheckPermission("drivers.create") is { } r) return r;
        await PopD(); return View(new DriverViewModel());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DriverViewModel vm) {
        if (CheckPermission("drivers.create") is { } r) return r;
        if (!ModelState.IsValid) { await PopD(); return View(vm); }
        await _svc.CreateAsync(ToEntity(vm)); TempData["Success"]=$"Driver {vm.FullName} added.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(string eid) {
        if (CheckPermission("drivers.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        var d=await _svc.GetByIdAsync(id); if(d==null) return NotFound();
        await PopD(); return View("Create",ToVM(d));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string eid, DriverViewModel vm) {
        if (CheckPermission("drivers.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        if (!ModelState.IsValid) { await PopD(); return View("Create",vm); }
        var d=await _svc.GetByIdAsync(id); if(d==null) return NotFound();
        d.FullName=vm.FullName;d.Phone=vm.Phone;d.Email=vm.Email;d.LicenseNumber=vm.LicenseNumber;
        d.LicenseExpiry=vm.LicenseExpiry;d.LicenseType=vm.LicenseType;d.Address=vm.Address;
        d.DateOfBirth=vm.DateOfBirth;d.AadharNumber=vm.AadharNumber;d.PanNumber=vm.PanNumber;
        d.Status=vm.Status;d.MonthlySalary=vm.MonthlySalary;d.BankAccountNumber=vm.BankAccountNumber;d.IFSC=vm.IFSC;
        await _svc.UpdateAsync(d); TempData["Success"]="Driver updated.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string eid) {
        if (CheckPermission("drivers.delete") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        await _svc.DeleteAsync(id); TempData["Success"]="Driver deleted.";
        return RedirectToAction(nameof(Index));
    }
    private Driver ToEntity(DriverViewModel vm) => new(){FullName=vm.FullName,Phone=vm.Phone,Email=vm.Email,
        LicenseNumber=vm.LicenseNumber,LicenseExpiry=vm.LicenseExpiry,LicenseType=vm.LicenseType,
        Address=vm.Address,DateOfBirth=vm.DateOfBirth,AadharNumber=vm.AadharNumber,PanNumber=vm.PanNumber,
        Status=vm.Status,MonthlySalary=vm.MonthlySalary,BankAccountNumber=vm.BankAccountNumber,IFSC=vm.IFSC,
        TenantId=_current.IsSuperAdmin?(vm.TenantId??0):_current.TenantId!.Value};
    private static DriverViewModel ToVM(Driver d) => new(){Id=d.Id,FullName=d.FullName,Phone=d.Phone,
        Email=d.Email,LicenseNumber=d.LicenseNumber,LicenseExpiry=d.LicenseExpiry,LicenseType=d.LicenseType,
        Address=d.Address,DateOfBirth=d.DateOfBirth,AadharNumber=d.AadharNumber,PanNumber=d.PanNumber,
        Status=d.Status,MonthlySalary=d.MonthlySalary,BankAccountNumber=d.BankAccountNumber,IFSC=d.IFSC,TenantId=d.TenantId};
    private async Task PopD() { if(_current.IsSuperAdmin) ViewBag.Tenants=new SelectList(await _db.Tenants.OrderBy(t=>t.CompanyName).ToListAsync(),"Id","CompanyName"); }
}

// TRIP
[Authorize] public class TripController : BaseController {
    private readonly ITripService _svc;
    private readonly IWebHostEnvironment _env;
    public TripController(ITripService svc, AppDbContext db, ICurrentTenantService c, IWebHostEnvironment env)
        : base(db,c) { _svc=svc; _env=env; }

    public async Task<IActionResult> Index(TripStatus? status, DateTime? from, DateTime? to, string? search) {
        var trips=await _svc.GetAllAsync(status,from,to);
        if (!string.IsNullOrWhiteSpace(search)) trips=trips.Where(t=>
            t.TripNumber.Contains(search,StringComparison.OrdinalIgnoreCase)||
            t.FromLocation.Contains(search,StringComparison.OrdinalIgnoreCase)||
            t.ToLocation.Contains(search,StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.StatusFilter=status; return View(trips);
    }
    public async Task<IActionResult> Details(string eid) {
        var id=DId(eid); if(id==0) return NotFound();
        var t=await _svc.GetByIdAsync(id); return t==null?NotFound():View(t);
    }
    public async Task<IActionResult> Create() {
        if (CheckPermission("trips.create") is { } r) return r;
        await PopDD(); return View(new TripViewModel{StartDate=DateTime.Today});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TripViewModel vm) {
        if (CheckPermission("trips.create") is { } r) return r;
        if (!ModelState.IsValid) { await PopDD(); return View(vm); }
        var trip=new Trip{TruckId=vm.TruckId,DriverId=vm.DriverId,FromLocation=vm.FromLocation,
            ToLocation=vm.ToLocation,StartDate=vm.StartDate,EndDate=vm.EndDate,DistanceKm=vm.DistanceKm,
            Revenue=vm.Revenue,CargoDescription=vm.CargoDescription,CargoWeightTons=vm.CargoWeightTons,
            ClientName=vm.ClientName,LRNumber=vm.LRNumber,Status=vm.Status,Notes=vm.Notes,
            TenantId=_current.TenantId??0};
        await _svc.CreateAsync(trip); TempData["Success"]=$"Trip {trip.TripNumber} created.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(string eid) {
        if (CheckPermission("trips.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        var t=await _svc.GetByIdAsync(id); if(t==null) return NotFound();
        await PopDD();
        return View("Create",new TripViewModel{Id=t.Id,TripNumber=t.TripNumber,TruckId=t.TruckId,
            DriverId=t.DriverId,FromLocation=t.FromLocation,ToLocation=t.ToLocation,
            StartDate=t.StartDate,EndDate=t.EndDate,DistanceKm=t.DistanceKm,Revenue=t.Revenue,
            CargoDescription=t.CargoDescription,CargoWeightTons=t.CargoWeightTons,
            ClientName=t.ClientName,LRNumber=t.LRNumber,Status=t.Status,Notes=t.Notes});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string eid, TripViewModel vm) {
        if (CheckPermission("trips.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        if (!ModelState.IsValid) { await PopDD(); return View("Create",vm); }
        var t=await _svc.GetByIdAsync(id); if(t==null) return NotFound();
        t.TruckId=vm.TruckId;t.DriverId=vm.DriverId;t.FromLocation=vm.FromLocation;t.ToLocation=vm.ToLocation;
        t.StartDate=vm.StartDate;t.EndDate=vm.EndDate;t.DistanceKm=vm.DistanceKm;t.Revenue=vm.Revenue;
        t.CargoDescription=vm.CargoDescription;t.CargoWeightTons=vm.CargoWeightTons;
        t.ClientName=vm.ClientName;t.LRNumber=vm.LRNumber;t.Status=vm.Status;t.Notes=vm.Notes;
        await _svc.UpdateAsync(t); TempData["Success"]="Trip updated.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string eid) {
        if (CheckPermission("trips.delete") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        await _svc.DeleteAsync(id); TempData["Success"]="Trip deleted.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(int tripId, IFormFile file, string documentType) {
        if (CheckPermission("trips.attachdocument") is { } r) return r;
        if (file==null||file.Length==0){TempData["Error"]="No file.";return RedirectToAction(nameof(Details),new{id=tripId});}
        var dir=Path.Combine(_env.WebRootPath,"uploads","trips",tripId.ToString());
        Directory.CreateDirectory(dir);
        var fn=$"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        await using(var s=new FileStream(Path.Combine(dir,fn),FileMode.Create)) await file.CopyToAsync(s);
        _db.TripDocuments.Add(new TripDocument{TripId=tripId,TenantId=_current.TenantId??0,FileName=file.FileName,
            FilePath=$"/uploads/trips/{tripId}/{fn}",FileType=Path.GetExtension(file.FileName),
            FileSizeBytes=file.Length,DocumentType=documentType});
        await _db.SaveChangesAsync();
        TempData["Success"]="Document uploaded."; return RedirectToAction(nameof(Details),new{id=tripId});
    }
    private async Task PopDD() {
        var tId=_current.TenantId;
        ViewBag.Trucks=new SelectList(await _db.Trucks.Where(t=>!t.IsDeleted&&(_current.IsSuperAdmin||t.TenantId==tId))
            .Select(t=>new{t.Id,Label=t.NumberPlate+" — "+t.Model}).ToListAsync(),"Id","Label");
        ViewBag.Drivers=new SelectList(await _db.Drivers.Where(d=>!d.IsDeleted&&(_current.IsSuperAdmin||d.TenantId==tId))
            .Select(d=>new{d.Id,d.FullName}).ToListAsync(),"Id","FullName");
    }
}

// EXPENSE
[Authorize] public class ExpenseController : BaseController {
    private readonly IExpenseService _svc;
    private readonly IWebHostEnvironment _env;
    public ExpenseController(IExpenseService svc, AppDbContext db, ICurrentTenantService c, IWebHostEnvironment env)
        : base(db,c) { _svc=svc; _env=env; }

    public async Task<IActionResult> Index(int? tripId, ExpenseCategory? category) {
        var expenses=await _svc.GetAllAsync(tripId,category);
        ViewBag.TripFilter=tripId; ViewBag.CategoryFilter=category;
        await PopE(); // Load trips dropdown for modal
        return View(expenses);
    }
    public async Task<IActionResult> Create(int? tripId) {
        if (CheckPermission("expenses.create") is { } r) return r;
        await PopE(); return View(new ExpenseViewModel{TripId=tripId??0,ExpenseDate=DateTime.Today});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseViewModel vm) {
        if (CheckPermission("expenses.create") is { } r) return r;
        if (!ModelState.IsValid){
            // If from modal, show errors and redirect back
            TempData["Error"]="Please fill all required fields.";
            return RedirectToAction(nameof(Index));
        }
        var e=new Expense{TripId=vm.TripId,Category=vm.Category,Amount=vm.Amount,ExpenseDate=vm.ExpenseDate,
            Description=vm.Description,VendorName=vm.VendorName,BillNumber=vm.BillNumber,TenantId=_current.TenantId??0};
        if (vm.Receipt is{Length:>0}){
            var dir=Path.Combine(_env.WebRootPath,"uploads","receipts"); Directory.CreateDirectory(dir);
            var fn=$"{Guid.NewGuid()}{Path.GetExtension(vm.Receipt.FileName)}";
            await using var s=new FileStream(Path.Combine(dir,fn),FileMode.Create); await vm.Receipt.CopyToAsync(s);
            e.ReceiptPath=$"/uploads/receipts/{fn}"; e.HasReceipt=true;
        }
        await _svc.CreateAsync(e); TempData["Success"]="Expense added.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(string eid) {
        if (CheckPermission("expenses.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        var e=await _svc.GetByIdAsync(id); if(e==null) return NotFound();
        await PopE();
        return View("Create",new ExpenseViewModel{Id=e.Id,TripId=e.TripId,Category=e.Category,Amount=e.Amount,
            ExpenseDate=e.ExpenseDate,Description=e.Description,VendorName=e.VendorName,BillNumber=e.BillNumber,
            ExistingReceiptPath=e.ReceiptPath});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string eid, ExpenseViewModel vm) {
        if (CheckPermission("expenses.edit") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        ModelState.Remove("Receipt");
        if (!ModelState.IsValid){await PopE();return View("Create",vm);}
        var e=await _svc.GetByIdAsync(id); if(e==null) return NotFound();
        e.Category=vm.Category;e.Amount=vm.Amount;e.ExpenseDate=vm.ExpenseDate;
        e.Description=vm.Description;e.VendorName=vm.VendorName;e.BillNumber=vm.BillNumber;
        if (vm.Receipt is{Length:>0}){
            var dir=Path.Combine(_env.WebRootPath,"uploads","receipts"); Directory.CreateDirectory(dir);
            var fn=$"{Guid.NewGuid()}{Path.GetExtension(vm.Receipt.FileName)}";
            await using var s=new FileStream(Path.Combine(dir,fn),FileMode.Create); await vm.Receipt.CopyToAsync(s);
            e.ReceiptPath=$"/uploads/receipts/{fn}"; e.HasReceipt=true;
        }
        await _svc.UpdateAsync(e); TempData["Success"]="Expense updated.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string eid) {
        if (CheckPermission("expenses.delete") is { } r) return r;
        var id=DId(eid); if(id==0) return NotFound();
        await _svc.DeleteAsync(id); TempData["Success"]="Expense deleted.";
        return RedirectToAction(nameof(Index));
    }
    private async Task PopE() {
        var tId=_current.TenantId;
        ViewBag.Trips=new SelectList(await _db.Trips.Where(t=>!t.IsDeleted&&(_current.IsSuperAdmin||t.TenantId==tId))
            .Select(t=>new{t.Id,Label=t.TripNumber+" — "+t.FromLocation+" → "+t.ToLocation})
            .OrderByDescending(t=>t.Id).ToListAsync(),"Id","Label");
    }
}

// ALERT
[Authorize] public class AlertController : BaseController {
    private readonly IAlertService _svc;
    public AlertController(IAlertService svc, AppDbContext db, ICurrentTenantService c) : base(db,c) => _svc=svc;
    public async Task<IActionResult> Index(AlertSeverity? severity) {
        ViewBag.SeverityFilter=severity; return View(await _svc.GetAlertsAsync(severity));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(string eid) {
        var id=DId(eid); if(id>0) await _svc.MarkReadAsync(id); return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles="SuperAdmin,TenantAdmin")]
    public async Task<IActionResult> Refresh() {
        await _svc.RefreshAlertsAsync(); TempData["Success"]="Alerts refreshed.";
        return RedirectToAction(nameof(Index));
    }
}

// REPORT
[Authorize] public class ReportController : BaseController {
    public ReportController(AppDbContext db, ICurrentTenantService c) : base(db,c) {}

    public IActionResult Index() => View(new ReportFilterViewModel{
        FromDate=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1), ToDate=DateTime.Today});

    [HttpPost]
    public async Task<IActionResult> PL(ReportFilterViewModel f) {
        var tId=_current.TenantId;
        var q=_current.IsSuperAdmin?_db.Trips.Include(t=>t.Expenses).AsQueryable()
            :_db.Trips.Include(t=>t.Expenses).Where(t=>t.TenantId==tId);
        if (f.FromDate.HasValue) q=q.Where(t=>t.StartDate>=f.FromDate);
        if (f.ToDate.HasValue) q=q.Where(t=>t.StartDate<=f.ToDate);
        var trips=await q.ToListAsync();
        var vm=new PLReportViewModel{
            TripRevenue=trips.Sum(t=>t.Revenue),
            FuelExpenses=trips.Sum(t=>t.Expenses.Where(e=>!e.IsDeleted&&e.Category==ExpenseCategory.Fuel).Sum(e=>e.Amount)),
            MaintenanceExpenses=trips.Sum(t=>t.Expenses.Where(e=>!e.IsDeleted&&e.Category==ExpenseCategory.Maintenance).Sum(e=>e.Amount)),
            TollExpenses=trips.Sum(t=>t.Expenses.Where(e=>!e.IsDeleted&&e.Category==ExpenseCategory.Toll).Sum(e=>e.Amount)),
            WagesExpenses=trips.Sum(t=>t.Expenses.Where(e=>!e.IsDeleted&&e.Category==ExpenseCategory.Wages).Sum(e=>e.Amount)),
            OtherExpenses=trips.Sum(t=>t.Expenses.Where(e=>!e.IsDeleted&&e.Category!=ExpenseCategory.Fuel
                &&e.Category!=ExpenseCategory.Maintenance&&e.Category!=ExpenseCategory.Toll
                &&e.Category!=ExpenseCategory.Wages).Sum(e=>e.Amount)),
            TripBreakdown=trips.Select(t=>new TripPLViewModel{TripNumber=t.TripNumber,
                Route=$"{t.FromLocation} → {t.ToLocation}",Revenue=t.Revenue,Expenses=t.TotalExpenses}).ToList()};
        vm.TotalRevenue=vm.TripRevenue+vm.OtherRevenue;
        vm.TotalExpenses=vm.FuelExpenses+vm.MaintenanceExpenses+vm.TollExpenses+vm.WagesExpenses+vm.OtherExpenses;
        return View("PL",vm);
    }
    public async Task<IActionResult> ExportTripsExcel(DateTime? from, DateTime? to) {
        var tId=_current.TenantId;
        var trips=await _db.Trips.Include(t=>t.Truck).Include(t=>t.Driver).Include(t=>t.Expenses)
            .Where(t=>!t.IsDeleted&&(_current.IsSuperAdmin||t.TenantId==tId))
            .Where(t=>(!from.HasValue||t.StartDate>=from)&&(!to.HasValue||t.StartDate<=to))
            .OrderByDescending(t=>t.StartDate).ToListAsync();
        using var wb=new ClosedXML.Excel.XLWorkbook();
        var ws=wb.Worksheets.Add("Trips");
        var hdr=new[]{"Trip#","From","To","Driver","Truck","Start","End","Dist(km)","Revenue","Expenses","Profit","Status"};
        for(int i=0;i<hdr.Length;i++){ws.Cell(1,i+1).Value=hdr[i];ws.Cell(1,i+1).Style.Font.Bold=true;
            ws.Cell(1,i+1).Style.Fill.BackgroundColor=ClosedXML.Excel.XLColor.DarkBlue;
            ws.Cell(1,i+1).Style.Font.FontColor=ClosedXML.Excel.XLColor.White;}
        for(int r=0;r<trips.Count;r++){var t=trips[r];int row=r+2;
            ws.Cell(row,1).Value=t.TripNumber;ws.Cell(row,2).Value=t.FromLocation;ws.Cell(row,3).Value=t.ToLocation;
            ws.Cell(row,4).Value=t.Driver?.FullName;ws.Cell(row,5).Value=t.Truck?.NumberPlate;
            ws.Cell(row,6).Value=t.StartDate.ToString("dd/MM/yyyy");ws.Cell(row,7).Value=t.EndDate?.ToString("dd/MM/yyyy");
            ws.Cell(row,8).Value=(double?)t.DistanceKm;ws.Cell(row,9).Value=(double)t.Revenue;
            ws.Cell(row,10).Value=(double)t.TotalExpenses;ws.Cell(row,11).Value=(double)t.NetProfit;ws.Cell(row,12).Value=t.Status.ToString();}
        ws.Columns().AdjustToContents();
        using var ms=new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"Trips_{DateTime.Today:yyyyMMdd}.xlsx");
    }
}

// ERROR
public class ErrorController : Controller {
    [Route("Error")] public IActionResult Index(int? code) { ViewBag.Code=code??500; return View(); }
}
