using System.Security.Claims;
using FleetPro.Data;
using FleetPro.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FleetPro.Services;

// ═══════════════════════════════════════════════════
//  CURRENT TENANT SERVICE  (Scoped)
// ═══════════════════════════════════════════════════
public interface ICurrentTenantService
{
    int? TenantId { get; }
    int UserId { get; }
    string UserEmail { get; }
    bool IsSuperAdmin { get; }
    string UserRole { get; }
    bool HasPermission(string permissionKey);
}

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _hca;
    private readonly AppDbContext _db;
    private List<string>? _cachedPermissions;

    public CurrentTenantService(IHttpContextAccessor hca, AppDbContext db)
    {
        _hca = hca;
        _db = db;
    }

    private ClaimsPrincipal? User => _hca.HttpContext?.User;

    public int? TenantId
    {
        get
        {
            var v = User?.FindFirstValue("TenantId");
            return v == null ? null : int.Parse(v);
        }
    }

    public int UserId
    {
        get
        {
            var v = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            return int.Parse(v);
        }
    }

    public string UserEmail => User?.FindFirstValue(ClaimTypes.Email) ?? "";
    public bool IsSuperAdmin => User?.IsInRole("SuperAdmin") ?? false;
    public string UserRole => User?.FindFirstValue(ClaimTypes.Role) ?? "";

    public bool HasPermission(string permissionKey)
    {
        if (IsSuperAdmin) return true;

        _cachedPermissions ??= User?
            .FindAll("Permission")
            .Select(c => c.Value)
            .ToList() ?? [];

        return _cachedPermissions.Contains(permissionKey);
    }
}

// ═══════════════════════════════════════════════════
//  AUTH SERVICE
// ═══════════════════════════════════════════════════
public interface IAuthService
{
    Task<(bool Success, ApplicationUser? User, string Error)> LoginAsync(string email, string password);
    Task<List<Claim>> BuildClaimsAsync(ApplicationUser user);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    public async Task<(bool Success, ApplicationUser? User, string Error)> LoginAsync(string email, string password)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null) return (false, null, "Invalid email or password.");
        if (user.Status == UserStatus.Inactive) return (false, null, "Account is inactive. Contact administrator.");
        if (user.Status == UserStatus.Locked) return (false, null, "Account is locked. Contact administrator.");
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return (false, null, "Invalid email or password.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, user, "");
    }

    public async Task<List<Claim>> BuildClaimsAsync(ApplicationUser user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // Load permissions via roles - materialize entire tables to avoid any SQL generation
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

        // Get all RolePermissions first (client-side filter)
        var allRolePermissions = await _db.RolePermissions.IgnoreQueryFilters().ToListAsync();
        var rolePermissionIds = allRolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsGranted)
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToList();

        // Get ALL permissions and filter in-memory
        var allPermissions = await _db.Permissions.IgnoreQueryFilters().ToListAsync();
        var rolePermissions = allPermissions
            .Where(p => rolePermissionIds.Contains(p.Id))
            .Select(p => p.Key)
            .ToList();

        // User-level overrides - split into separate queries
        var allUserPermissions = await _db.UserPermissions.IgnoreQueryFilters().ToListAsync();
        var userPermissionIds = allUserPermissions
            .Where(up => up.UserId == user.Id)
            .Select(up => new { up.PermissionId, up.IsGranted })
            .ToList();

        var userPermissionIdsList = userPermissionIds.Select(u => u.PermissionId).ToList();
        var grantedByUser = allPermissions
            .Where(p => userPermissionIdsList.Contains(p.Id))
            .Where(p => userPermissionIds.Any(up => up.PermissionId == p.Id && up.IsGranted))
            .Select(p => p.Key);
        var deniedByUser = allPermissions
            .Where(p => userPermissionIds.Any(up => up.PermissionId == p.Id && !up.IsGranted))
            .Select(p => p.Key)
            .ToList();

        var finalPermissions = rolePermissions
            .Union(grantedByUser)
            .Where(p => !deniedByUser.Contains(p))
            .Distinct()
            .ToList();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
        };

        if (user.TenantId.HasValue)
            claims.Add(new("TenantId", user.TenantId.Value.ToString()));

        foreach (var role in roles)
            claims.Add(new(ClaimTypes.Role, role));

        foreach (var perm in finalPermissions)
            claims.Add(new("Permission", perm));

        return claims;
    }
}

// ═══════════════════════════════════════════════════
//  TENANT SERVICE
// ═══════════════════════════════════════════════════
public interface ITenantService
{
    Task<List<Tenant>> GetAllAsync();
    Task<Tenant?> GetByIdAsync(int id);
    Task<Tenant> CreateAsync(Tenant tenant, ApplicationUser owner);
    Task<Tenant> UpdateAsync(Tenant tenant);
    Task DeleteAsync(int id);
}

public class TenantService : ITenantService
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantService _current;

    public TenantService(AppDbContext db, ICurrentTenantService current)
    { _db = db; _current = current; }

    public async Task<List<Tenant>> GetAllAsync() =>
        await _db.Tenants.OrderBy(t => t.CompanyName).ToListAsync();

    public async Task<Tenant?> GetByIdAsync(int id) =>
        await _db.Tenants.Include(t => t.Users).Include(t => t.Trucks)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tenant> CreateAsync(Tenant tenant, ApplicationUser owner)
    {
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        owner.TenantId = tenant.Id;
        owner.PasswordHash = BCrypt.Net.BCrypt.HashPassword(owner.PasswordHash);
        _db.Users.Add(owner);
        await _db.SaveChangesAsync();

        var adminRole = await _db.Roles.FirstAsync(r => r.Name == "TenantAdmin");
        _db.UserRoles.Add(new UserRole { UserId = owner.Id, RoleId = adminRole.Id });
        await _db.SaveChangesAsync();

        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant)
    {
        tenant.UpdatedAt = DateTime.UtcNow;
        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync();
        return tenant;
    }

    public async Task DeleteAsync(int id)
    {
        var t = await _db.Tenants.FindAsync(id);
        if (t != null) { t.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}

// ═══════════════════════════════════════════════════
//  USER SERVICE
// ═══════════════════════════════════════════════════
public interface IUserService
{
    Task<List<ApplicationUser>> GetUsersAsync(int? tenantId = null);
    Task<ApplicationUser?> GetByIdAsync(int id);
    Task<ApplicationUser> CreateAsync(ApplicationUser user, int roleId);
    Task UpdateAsync(ApplicationUser user);
    Task DeleteAsync(int id);
    Task UpdatePermissionsAsync(int userId, Dictionary<int, bool> permissions);
    Task<List<UserPermission>> GetUserPermissionsAsync(int userId);
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantService _current;

    public UserService(AppDbContext db, ICurrentTenantService current)
    { _db = db; _current = current; }

    public async Task<List<ApplicationUser>> GetUsersAsync(int? tenantId = null)
    {
        var q = _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();
        if (tenantId.HasValue) q = q.Where(u => u.TenantId == tenantId);
        else if (!_current.IsSuperAdmin && _current.TenantId.HasValue)
            q = q.Where(u => u.TenantId == _current.TenantId);
        return await q.OrderBy(u => u.FullName).ToListAsync();
    }

    public async Task<ApplicationUser?> GetByIdAsync(int id)
    {
        var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user != null)
        {
            // Materialize all user permissions and permissions to avoid CTE generation
            var allUserPerms = await _db.UserPermissions.IgnoreQueryFilters().ToListAsync();
            var userPerms = allUserPerms.Where(up => up.UserId == id).ToList();

            if (userPerms.Any())
            {
                var allPermissions = await _db.Permissions.IgnoreQueryFilters().ToListAsync();

                user.UserPermissions = userPerms
                    .Select(up => new UserPermission 
                    { 
                        Id = up.Id,
                        UserId = up.UserId,
                        PermissionId = up.PermissionId,
                        Permission = allPermissions.FirstOrDefault(p => p.Id == up.PermissionId),
                        IsGranted = up.IsGranted,
                        IsDeleted = up.IsDeleted,
                        CreatedAt = up.CreatedAt,
                        UpdatedAt = up.UpdatedAt
                    })
                    .ToList();
            }
        }

        return user;
    }

    public async Task<ApplicationUser> CreateAsync(ApplicationUser user, int roleId)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(ApplicationUser user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u != null) { u.IsDeleted = true; await _db.SaveChangesAsync(); }
    }

    public async Task UpdatePermissionsAsync(int userId, Dictionary<int, bool> permissions)
    {
        var existing = await _db.UserPermissions.Where(up => up.UserId == userId).ToListAsync();
        _db.UserPermissions.RemoveRange(existing);
        foreach (var kv in permissions)
            _db.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = kv.Key, IsGranted = kv.Value });
        await _db.SaveChangesAsync();
    }

    public async Task<List<UserPermission>> GetUserPermissionsAsync(int userId)
    {
        // Materialize all tables to avoid any CTE generation
        var allUserPerms = await _db.UserPermissions.IgnoreQueryFilters().ToListAsync();
        var userPerms = allUserPerms
            .Where(up => up.UserId == userId)
            .Select(up => new { up.Id, up.PermissionId, up.IsGranted, up.CreatedAt, up.UpdatedAt, up.IsDeleted, up.CreatedBy, up.UpdatedBy })
            .ToList();

        if (!userPerms.Any())
            return [];

        var allPermissions = await _db.Permissions.IgnoreQueryFilters().ToListAsync();

        return userPerms
            .Where(up => allPermissions.Any(p => p.Id == up.PermissionId))
            .Select(up => 
            {
                var permission = allPermissions.FirstOrDefault(p => p.Id == up.PermissionId);
                return new UserPermission
                {
                    Id = up.Id,
                    UserId = userId,
                    PermissionId = up.PermissionId,
                    Permission = permission,
                    IsGranted = up.IsGranted,
                    CreatedAt = up.CreatedAt,
                    UpdatedAt = up.UpdatedAt,
                    IsDeleted = up.IsDeleted,
                    CreatedBy = up.CreatedBy,
                    UpdatedBy = up.UpdatedBy
                };
            })
            .ToList();
    }
}

// ═══════════════════════════════════════════════════
//  TRUCK SERVICE
// ═══════════════════════════════════════════════════
public interface ITruckService
{
    Task<List<Truck>> GetAllAsync();
    Task<Truck?> GetByIdAsync(int id);
    Task<Truck> CreateAsync(Truck truck);
    Task UpdateAsync(Truck truck);
    Task DeleteAsync(int id);
}

public class TruckService : ITruckService
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantService _current;

    public TruckService(AppDbContext db, ICurrentTenantService current)
    { _db = db; _current = current; }

    private IQueryable<Truck> Query() =>
        _current.IsSuperAdmin
            ? _db.Trucks.Include(t => t.Tenant)
            : _db.Trucks.Where(t => t.TenantId == _current.TenantId);

    public async Task<List<Truck>> GetAllAsync() =>
        await Query().OrderBy(t => t.NumberPlate).ToListAsync();

    public async Task<Truck?> GetByIdAsync(int id) =>
        await Query().FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Truck> CreateAsync(Truck truck)
    {
        // Ensure TenantId is always set
        if (truck.TenantId <= 0)
        {
            if (_current.IsSuperAdmin)
                throw new InvalidOperationException("TenantId must be provided for SuperAdmin users.");

            truck.TenantId = _current.TenantId!.Value;
        }

        // Validate that the tenant exists
        var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == truck.TenantId);
        if (!tenantExists)
            throw new InvalidOperationException($"Tenant with ID {truck.TenantId} does not exist.");

        _db.Trucks.Add(truck);
        await _db.SaveChangesAsync();
        return truck;
    }

    public async Task UpdateAsync(Truck truck)
    {
        truck.UpdatedAt = DateTime.UtcNow;
        _db.Trucks.Update(truck);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var t = await _db.Trucks.FindAsync(id);
        if (t != null) { t.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}

// ═══════════════════════════════════════════════════
//  DRIVER SERVICE
// ═══════════════════════════════════════════════════
public interface IDriverService
{
    Task<List<Driver>> GetAllAsync();
    Task<Driver?> GetByIdAsync(int id);
    Task<Driver> CreateAsync(Driver driver);
    Task UpdateAsync(Driver driver);
    Task DeleteAsync(int id);
}

public class DriverService : IDriverService
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantService _current;

    public DriverService(AppDbContext db, ICurrentTenantService current)
    { _db = db; _current = current; }

    private IQueryable<Driver> Query() =>
        _current.IsSuperAdmin
            ? _db.Drivers.Include(d => d.Tenant)
            : _db.Drivers.Where(d => d.TenantId == _current.TenantId);

    public async Task<List<Driver>> GetAllAsync() =>
        await Query().OrderBy(d => d.FullName).ToListAsync();

    public async Task<Driver?> GetByIdAsync(int id) =>
        await Query().FirstOrDefaultAsync(d => d.Id == id);

    public async Task<Driver> CreateAsync(Driver driver)
    {
        // Ensure TenantId is always set
        if (driver.TenantId <= 0)
        {
            if (_current.IsSuperAdmin)
                throw new InvalidOperationException("TenantId must be provided for SuperAdmin users.");

            driver.TenantId = _current.TenantId!.Value;
        }

        // Validate that the tenant exists
        var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == driver.TenantId);
        if (!tenantExists)
            throw new InvalidOperationException($"Tenant with ID {driver.TenantId} does not exist.");

        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync();
        return driver;
    }

    public async Task UpdateAsync(Driver driver)
    {
        driver.UpdatedAt = DateTime.UtcNow;
        _db.Drivers.Update(driver);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var d = await _db.Drivers.FindAsync(id);
        if (d != null) { d.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}

// ═══════════════════════════════════════════════════
//  TRIP SERVICE
// ═══════════════════════════════════════════════════
public interface ITripService
{
    Task<List<Trip>> GetAllAsync(TripStatus? status = null, DateTime? from = null, DateTime? to = null);
    Task<Trip?> GetByIdAsync(int id);
    Task<Trip> CreateAsync(Trip trip);
    Task UpdateAsync(Trip trip);
    Task DeleteAsync(int id);
    Task<string> GenerateTripNumberAsync();
}

public class TripService : ITripService
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantService _current;

    public TripService(AppDbContext db, ICurrentTenantService current)
    { _db = db; _current = current; }

    private IQueryable<Trip> Query() =>
        _current.IsSuperAdmin
            ? _db.Trips.Include(t => t.Truck).Include(t => t.Driver).Include(t => t.Tenant).Include(t => t.Expenses)
            : _db.Trips.Include(t => t.Truck).Include(t => t.Driver).Include(t => t.Expenses)
                .Where(t => t.TenantId == _current.TenantId);

    public async Task<List<Trip>> GetAllAsync(TripStatus? status = null, DateTime? from = null, DateTime? to = null)
    {
        var q = Query();
        if (status.HasValue) q = q.Where(t => t.Status == status);
        if (from.HasValue) q = q.Where(t => t.StartDate >= from);
        if (to.HasValue) q = q.Where(t => t.StartDate <= to);
        return await q.OrderByDescending(t => t.StartDate).ToListAsync();
    }

    public async Task<Trip?> GetByIdAsync(int id) =>
        await Query().Include(t => t.Documents).FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Trip> CreateAsync(Trip trip)
    {
        // Ensure TenantId is always set
        if (trip.TenantId <= 0)
        {
            if (_current.IsSuperAdmin)
                throw new InvalidOperationException("TenantId must be provided for SuperAdmin users.");

            trip.TenantId = _current.TenantId!.Value;
        }

        // Validate that the tenant exists
        var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == trip.TenantId);
        if (!tenantExists)
            throw new InvalidOperationException($"Tenant with ID {trip.TenantId} does not exist.");

        trip.TripNumber = await GenerateTripNumberAsync();
        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();
        return trip;
    }

    public async Task UpdateAsync(Trip trip)
    {
        trip.UpdatedAt = DateTime.UtcNow;
        _db.Trips.Update(trip);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var t = await _db.Trips.FindAsync(id);
        if (t != null) { t.IsDeleted = true; await _db.SaveChangesAsync(); }
    }

    public async Task<string> GenerateTripNumberAsync()
    {
        var tenantId = _current.TenantId;
        if (tenantId == null || tenantId <= 0)
            throw new InvalidOperationException("TenantId is required to generate trip number.");

        var count = await _db.Trips
            .Where(t => t.TenantId == tenantId)
            .CountAsync();
        return $"T-{1000 + count + 1}";
    }
}

// ═══════════════════════════════════════════════════
//  EXPENSE SERVICE
// ═══════════════════════════════════════════════════
public interface IExpenseService
{
    Task<List<Expense>> GetAllAsync(int? tripId = null, ExpenseCategory? category = null);
    Task<Expense?> GetByIdAsync(int id);
    Task<Expense> CreateAsync(Expense expense);
    Task UpdateAsync(Expense expense);
    Task DeleteAsync(int id);
}

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantService _current;

    public ExpenseService(AppDbContext db, ICurrentTenantService current)
    { _db = db; _current = current; }

    private IQueryable<Expense> Query() =>
        _current.IsSuperAdmin
            ? _db.Expenses.Include(e => e.Trip)
            : _db.Expenses.Include(e => e.Trip).Where(e => e.TenantId == _current.TenantId);

    public async Task<List<Expense>> GetAllAsync(int? tripId = null, ExpenseCategory? category = null)
    {
        var q = Query();
        if (tripId.HasValue) q = q.Where(e => e.TripId == tripId);
        if (category.HasValue) q = q.Where(e => e.Category == category);
        return await q.OrderByDescending(e => e.ExpenseDate).ToListAsync();
    }

    public async Task<Expense?> GetByIdAsync(int id) =>
        await Query().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Expense> CreateAsync(Expense expense)
    {
        // Ensure TenantId is always set
        if (expense.TenantId <= 0)
        {
            if (_current.IsSuperAdmin)
                throw new InvalidOperationException("TenantId must be provided for SuperAdmin users.");

            expense.TenantId = _current.TenantId!.Value;
        }

        // Validate that the tenant exists
        var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == expense.TenantId);
        if (!tenantExists)
            throw new InvalidOperationException($"Tenant with ID {expense.TenantId} does not exist.");

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        return expense;
    }

    public async Task UpdateAsync(Expense expense)
    {
        expense.UpdatedAt = DateTime.UtcNow;
        _db.Expenses.Update(expense);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.Expenses.FindAsync(id);
        if (e != null) { e.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}

// ═══════════════════════════════════════════════════
//  ALERT SERVICE
// ═══════════════════════════════════════════════════
public interface IAlertService
{
    Task<List<Alert>> GetAlertsAsync(AlertSeverity? severity = null);
    Task RefreshAlertsAsync();
    Task MarkReadAsync(int id);
}

public class AlertService : IAlertService
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantService _current;

    public AlertService(AppDbContext db, ICurrentTenantService current)
    { _db = db; _current = current; }

    public async Task<List<Alert>> GetAlertsAsync(AlertSeverity? severity = null)
    {
        var q = _current.IsSuperAdmin
            ? _db.Alerts.AsQueryable()
            : _db.Alerts.Where(a => a.TenantId == _current.TenantId);
        if (severity.HasValue) q = q.Where(a => a.Severity == severity);
        return await q.OrderBy(a => a.DaysRemaining).ToListAsync();
    }

    public async Task RefreshAlertsAsync()
    {
        var today = DateTime.Today;
        var thresholdWarning = today.AddDays(30);
        var thresholdInfo = today.AddDays(60);

        // Trucks
        var trucks = await _db.Trucks.Where(t => !t.IsDeleted).ToListAsync();
        foreach (var truck in trucks)
        {
            await UpsertAlertAsync(truck.Id, "Truck", AlertType.TruckInsuranceExpiry,
                $"Insurance Expiry — {truck.NumberPlate}", truck.InsuranceExpiry);
            await UpsertAlertAsync(truck.Id, "Truck", AlertType.TruckFitnessExpiry,
                $"Fitness Expiry — {truck.NumberPlate}", truck.FitnessExpiry);
        }

        // Drivers
        var drivers = await _db.Drivers.Where(d => !d.IsDeleted).ToListAsync();
        foreach (var driver in drivers)
        {
            await UpsertAlertAsync(driver.Id, "Driver", AlertType.DriverLicenseExpiry,
                $"License Expiry — {driver.FullName}", driver.LicenseExpiry);
        }

        await _db.SaveChangesAsync();
    }

    private async Task UpsertAlertAsync(int refId, string refType, AlertType type, string title, DateTime? expiry)
    {
        if (!expiry.HasValue) return;
        var days = (expiry.Value.Date - DateTime.Today).Days;
        if (days > 60) return;   // no alert needed yet

        var severity = days <= 7 ? AlertSeverity.Critical
                     : days <= 30 ? AlertSeverity.Warning
                     : AlertSeverity.Info;

        // find or create
        var alert = await _db.Alerts.FirstOrDefaultAsync(a =>
            a.ReferenceId == refId && a.ReferenceType == refType && a.Type == type);

        if (alert == null)
        {
            var entity = refType == "Truck"
                ? (int?)await _db.Trucks.Where(t => t.Id == refId).Select(t => t.TenantId).FirstOrDefaultAsync()
                : await _db.Drivers.Where(d => d.Id == refId).Select(d => (int?)d.TenantId).FirstOrDefaultAsync();

            _db.Alerts.Add(new Alert
            {
                TenantId = entity ?? 0,
                Type = type,
                Severity = severity,
                Title = title,
                ReferenceId = refId,
                ReferenceType = refType,
                ExpiryDate = expiry.Value,
                DaysRemaining = days
            });
        }
        else
        {
            alert.Severity = severity;
            alert.DaysRemaining = days;
            alert.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task MarkReadAsync(int id)
    {
        var a = await _db.Alerts.FindAsync(id);
        if (a != null) { a.IsRead = true; await _db.SaveChangesAsync(); }
    }
}

// ═══════════════════════════════════════════════════
//  DASHBOARD SERVICE
// ═══════════════════════════════════════════════════
public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
}

public class DashboardStats
{
    public int TotalTenants { get; set; }
    public int ActiveTrucks { get; set; }
    public int TotalTrucks { get; set; }
    public int ActiveTrips { get; set; }
    public int TotalDrivers { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int CriticalAlerts { get; set; }
    public int TotalAlerts { get; set; }
    public List<Trip> RecentTrips { get; set; } = [];
    public List<Alert> UrgentAlerts { get; set; } = [];
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantService _current;

    public DashboardService(AppDbContext db, ICurrentTenantService current)
    { _db = db; _current = current; }

    public async Task<DashboardStats> GetStatsAsync()
    {
        var tenantId = _current.TenantId;
        var isSuperAdmin = _current.IsSuperAdmin;
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var truckQ = isSuperAdmin ? _db.Trucks : _db.Trucks.Where(t => t.TenantId == tenantId);
        var driverQ = isSuperAdmin ? _db.Drivers : _db.Drivers.Where(d => d.TenantId == tenantId);
        var tripQ = isSuperAdmin ? _db.Trips.Include(t => t.Expenses).Include(t => t.Truck).Include(t => t.Driver)
                                 : _db.Trips.Include(t => t.Expenses).Include(t => t.Truck).Include(t => t.Driver)
                                     .Where(t => t.TenantId == tenantId);
        var alertQ = isSuperAdmin ? _db.Alerts : _db.Alerts.Where(a => a.TenantId == tenantId);

        var monthlyTrips = await tripQ.Where(t => t.StartDate >= monthStart).ToListAsync();
        var monthlyRevenue = monthlyTrips.Sum(t => t.Revenue);
        var monthlyExpenses = monthlyTrips.Sum(t => t.TotalExpenses);

        return new DashboardStats
        {
            TotalTenants = isSuperAdmin ? await _db.Tenants.CountAsync() : 1,
            TotalTrucks = await truckQ.CountAsync(),
            ActiveTrucks = await truckQ.CountAsync(t => t.Status == TruckStatus.Active),
            ActiveTrips = await tripQ.CountAsync(t => t.Status == TripStatus.InProgress),
            TotalDrivers = await driverQ.CountAsync(),
            MonthlyRevenue = monthlyRevenue,
            MonthlyExpenses = monthlyExpenses,
            NetProfit = monthlyRevenue - monthlyExpenses,
            CriticalAlerts = await alertQ.CountAsync(a => a.Severity == AlertSeverity.Critical && !a.IsRead),
            TotalAlerts = await alertQ.CountAsync(a => !a.IsRead),
            RecentTrips = await tripQ.OrderByDescending(t => t.StartDate).Take(10).ToListAsync(),
            UrgentAlerts = await alertQ.Where(a => !a.IsRead)
                .OrderBy(a => a.DaysRemaining).Take(5).ToListAsync()
        };
    }
}
