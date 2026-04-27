using BCrypt.Net;
using FleetPro.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FleetPro.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();
        await EnsureMenuItemsTableAsync(db);
        await SeedPermissionsAsync(db);
        await SeedRolePermissionsAsync(db);
        await SeedSuperAdminAsync(db);
        await SeedSampleTenantAsync(db);
        await SeedMenuItemsAsync(db);
    }

    /// <summary>
    /// Ensures the MenuItems table exists (for DBs created before migrations).
    /// </summary>
    private static async Task EnsureMenuItemsTableAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MenuItems')
            BEGIN
                CREATE TABLE [MenuItems] (
                    [Id] int NOT NULL IDENTITY,
                    [Title] nvarchar(100) NOT NULL,
                    [Icon] nvarchar(max) NULL,
                    [Controller] nvarchar(max) NULL,
                    [Action] nvarchar(max) NULL,
                    [ParentId] int NULL,
                    [SortOrder] int NOT NULL,
                    [RequiredPermission] nvarchar(max) NULL,
                    [SuperAdminOnly] bit NOT NULL DEFAULT 0,
                    [TenantAdminOrAbove] bit NOT NULL DEFAULT 0,
                    [IsActive] bit NOT NULL DEFAULT 1,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] datetime2 NULL,
                    [IsDeleted] bit NOT NULL DEFAULT 0,
                    [CreatedBy] int NULL,
                    [UpdatedBy] int NULL,
                    CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_MenuItems_MenuItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_MenuItems_ParentId] ON [MenuItems] ([ParentId]);
            END
        ");
    }

    // Public so it can be called on-demand from RoleController
    public static async Task SeedRolePermissionsAsync(AppDbContext db)
    {
        await SeedPermissionsAsync(db);  // ensure permissions exist first
        await SeedRolesAsync(db);
    }

    private static async Task SeedPermissionsAsync(AppDbContext db)
    {
        var modules = new[]
        {
            ("Tenants",  new[] { "View","Create","Edit","Delete" }),
            ("Users",    new[] { "View","Create","Edit","Delete","AssignRoles" }),
            ("Trucks",   new[] { "View","Create","Edit","Delete" }),
            ("Drivers",  new[] { "View","Create","Edit","Delete" }),
            ("Trips",    new[] { "View","Create","Edit","Delete","AttachDocument" }),
            ("Expenses", new[] { "View","Create","Edit","Delete","AttachBill" }),
            ("Reports",  new[] { "View","Export" }),
            ("Alerts",   new[] { "View","Manage" }),
            ("Dashboard",new[] { "View" }),
        };

        var existingKeys = await db.Permissions.Select(p => p.Key).ToHashSetAsync();

        var toAdd = new List<Permission>();
        foreach (var (module, actions) in modules)
            foreach (var action in actions)
            {
                var key = $"{module.ToLower()}.{action.ToLower()}";
                if (!existingKeys.Contains(key))
                    toAdd.Add(new Permission {
                        Module = module, Action = action,
                        Key = key, Description = $"{action} {module}"
                    });
            }

        if (toAdd.Count > 0)
        {
            db.Permissions.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedRolesAsync(AppDbContext db)
    {
        // ── Ensure all 4 roles exist ──────────────────────────────
        var roleDefs = new[]
        {
            ("SuperAdmin",        "Full system access"),
            ("TenantAdmin",       "Full access within their tenant"),
            ("DataEntryOperator", "Create and edit, no delete"),
            ("Viewer",            "Read-only access"),
        };

        foreach (var (name, desc) in roleDefs)
            if (!await db.Roles.AnyAsync(r => r.Name == name))
                db.Roles.Add(new Role { Name = name, Description = desc, IsSystemRole = true });

        await db.SaveChangesAsync();

        // ── Ensure role permissions are complete ──────────────────
        var allPerms    = await db.Permissions.ToListAsync();
        var superAdmin  = await db.Roles.FirstAsync(r => r.Name == "SuperAdmin");
        var tenantAdmin = await db.Roles.FirstAsync(r => r.Name == "TenantAdmin");
        var dataEntry   = await db.Roles.FirstAsync(r => r.Name == "DataEntryOperator");
        var viewer      = await db.Roles.FirstAsync(r => r.Name == "Viewer");

        // Load existing role-permission pairs to avoid duplicates
        var existingRpList = await db.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync();
        var existing = existingRpList
            .Select(rp => (rp.RoleId, rp.PermissionId))
            .ToHashSet();

        // Also clean up any existing duplicates in DB
        var duplicates = await db.RolePermissions
            .GroupBy(rp => new { rp.RoleId, rp.PermissionId })
            .Where(g => g.Count() > 1)
            .ToListAsync();
        foreach (var grp in duplicates)
        {
            var toRemove = grp.Skip(1).ToList();
            db.RolePermissions.RemoveRange(
                await db.RolePermissions
                    .Where(rp => rp.RoleId == grp.Key.RoleId && rp.PermissionId == grp.Key.PermissionId)
                    .OrderBy(rp => rp.Id).Skip(1).ToListAsync()
            );
        }
        if (duplicates.Any()) await db.SaveChangesAsync();

        var toAdd = new List<RolePermission>();

        void AddIfMissing(int roleId, IEnumerable<Permission> perms)
        {
            foreach (var p in perms)
                if (!existing.Contains((roleId, p.Id)))
                    toAdd.Add(new RolePermission { RoleId = roleId, PermissionId = p.Id, IsGranted = true });
        }

        // SuperAdmin — ALL
        AddIfMissing(superAdmin.Id, allPerms);

        // TenantAdmin — all except Tenants.Create / Tenants.Delete
        AddIfMissing(tenantAdmin.Id,
            allPerms.Where(p => !(p.Module == "Tenants" && p.Action is "Create" or "Delete")));

        // DataEntryOperator — View + Create + Edit + AttachDocument + AttachBill
        AddIfMissing(dataEntry.Id,
            allPerms.Where(p => p.Action is "View" or "Create" or "Edit" or "AttachDocument" or "AttachBill"));

        // Viewer — View only
        AddIfMissing(viewer.Id,
            allPerms.Where(p => p.Action == "View"));

        if (toAdd.Count > 0)
        {
            db.RolePermissions.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedSuperAdminAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Email == "superadmin@fleetpro.in")) return;

        var superAdmin = new ApplicationUser
        {
            FullName     = "Super Administrator",
            Email        = "superadmin@fleetpro.in",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("FleetPro@2025!"),
            Phone        = "+91 90000 00000",
            Status       = UserStatus.Active
        };
        db.Users.Add(superAdmin);
        await db.SaveChangesAsync();

        var superAdminRole = await db.Roles.FirstAsync(r => r.Name == "SuperAdmin");
        db.UserRoles.Add(new UserRole { UserId=superAdmin.Id, RoleId=superAdminRole.Id });
        await db.SaveChangesAsync();
    }

    private static async Task SeedSampleTenantAsync(AppDbContext db)
    {
        if (await db.Tenants.AnyAsync()) return;

        var tenant1 = new Tenant
        {
            CompanyName="Sharma Transport Pvt. Ltd.", Subdomain="sharma-transport",
            ContactPerson="Ravi Kumar Sharma", Email="ravi@sharmatransport.in",
            Phone="+91 98765 43210", GstNumber="27AAAAA0000A1Z5",
            Address="123, Industrial Area, Kurla", City="Mumbai", State="Maharashtra",
            Plan=TenantPlan.Premium, Status=TenantStatus.Active,
            MaxTrucks=999, MaxUsers=999,
            SubscriptionStartDate=DateTime.UtcNow.AddMonths(-6),
            SubscriptionEndDate=DateTime.UtcNow.AddMonths(6)
        };
        var tenant2 = new Tenant
        {
            CompanyName="Gupta Logistics Services", Subdomain="gupta-logistics",
            ContactPerson="Manoj Gupta", Email="manoj@guptalogistics.in",
            Phone="+91 87654 32109", City="Delhi", State="Delhi",
            Plan=TenantPlan.Business, Status=TenantStatus.Active,
            MaxTrucks=25, MaxUsers=10,
            SubscriptionStartDate=DateTime.UtcNow.AddMonths(-3),
            SubscriptionEndDate=DateTime.UtcNow.AddMonths(9)
        };
        db.Tenants.AddRange(tenant1, tenant2);
        await db.SaveChangesAsync();

        var tenantAdminRole = await db.Roles.FirstAsync(r => r.Name == "TenantAdmin");
        var dataEntryRole   = await db.Roles.FirstAsync(r => r.Name == "DataEntryOperator");

        var t1Owner = new ApplicationUser
        {
            FullName="Ravi Kumar Sharma", Email="ravi@sharmatransport.in",
            PasswordHash=BCrypt.Net.BCrypt.HashPassword("Sharma@2025!"),
            Phone="+91 98765 43210", TenantId=tenant1.Id, Status=UserStatus.Active
        };
        var t1DataEntry = new ApplicationUser
        {
            FullName="Anita Patel", Email="anita@sharmatransport.in",
            PasswordHash=BCrypt.Net.BCrypt.HashPassword("Anita@2025!"),
            TenantId=tenant1.Id, Status=UserStatus.Active
        };
        var t2Owner = new ApplicationUser
        {
            FullName="Manoj Gupta", Email="manoj@guptalogistics.in",
            PasswordHash=BCrypt.Net.BCrypt.HashPassword("Gupta@2025!"),
            Phone="+91 87654 32109", TenantId=tenant2.Id, Status=UserStatus.Active
        };
        db.Users.AddRange(t1Owner, t1DataEntry, t2Owner);
        await db.SaveChangesAsync();

        db.UserRoles.AddRange(
            new UserRole { UserId=t1Owner.Id,    RoleId=tenantAdminRole.Id },
            new UserRole { UserId=t1DataEntry.Id, RoleId=dataEntryRole.Id },
            new UserRole { UserId=t2Owner.Id,    RoleId=tenantAdminRole.Id }
        );

        // Sample Trucks
        var trucks = new List<Truck>
        {
            new() { TenantId=tenant1.Id, NumberPlate="MH12AB1234", Model="Tata Prima 4928S",
                    Make="Tata", ManufacturingYear=2021,
                    FitnessExpiry=DateTime.Today.AddDays(15), InsuranceExpiry=DateTime.Today.AddDays(5),
                    Status=TruckStatus.Active, LoadCapacityTons=25 },
            new() { TenantId=tenant1.Id, NumberPlate="MH14CD5678", Model="Ashok Leyland 2518",
                    Make="Ashok Leyland", ManufacturingYear=2020,
                    FitnessExpiry=DateTime.Today.AddMonths(3), InsuranceExpiry=DateTime.Today.AddMonths(4),
                    Status=TruckStatus.Active, LoadCapacityTons=18 },
            new() { TenantId=tenant2.Id, NumberPlate="KA05EF9012", Model="Mahindra Blazo X 35",
                    Make="Mahindra", ManufacturingYear=2022,
                    FitnessExpiry=DateTime.Today.AddDays(2), InsuranceExpiry=DateTime.Today.AddMonths(7),
                    Status=TruckStatus.InMaintenance, LoadCapacityTons=35 },
        };
        db.Trucks.AddRange(trucks);

        // Sample Drivers
        var drivers = new List<Driver>
        {
            new() { TenantId=tenant1.Id, FullName="Rajesh Kumar", Phone="+91 98765 43210",
                    LicenseNumber="MH0219800012345", LicenseExpiry=DateTime.Today.AddDays(12),
                    LicenseType="HMV", Status=DriverStatus.Active, MonthlySalary=25000 },
            new() { TenantId=tenant1.Id, FullName="Suresh Patel", Phone="+91 87654 32109",
                    LicenseNumber="GJ0119900054321", LicenseExpiry=DateTime.Today.AddDays(-1),
                    LicenseType="HMV", Status=DriverStatus.Active, MonthlySalary=22000 },
            new() { TenantId=tenant2.Id, FullName="Amit Singh", Phone="+91 76543 21098",
                    LicenseNumber="KA0120010067890", LicenseExpiry=DateTime.Today.AddMonths(8),
                    LicenseType="HMV", Status=DriverStatus.Active, MonthlySalary=24000 },
        };
        db.Drivers.AddRange(drivers);
        await db.SaveChangesAsync();

        // Sample Trips
        var trip1 = new Trip
        {
            TenantId=tenant1.Id, TripNumber="T-1001", TruckId=trucks[0].Id, DriverId=drivers[0].Id,
            FromLocation="Mumbai, Maharashtra", ToLocation="Delhi, NCR",
            StartDate=DateTime.Today.AddDays(-10), EndDate=DateTime.Today.AddDays(-8),
            DistanceKm=1421, Revenue=45000, CargoDescription="FMCG Goods",
            CargoWeightTons=20, ClientName="Reliance Retail", LRNumber="LR-2025-001",
            Status=TripStatus.Completed
        };
        var trip2 = new Trip
        {
            TenantId=tenant1.Id, TripNumber="T-1002", TruckId=trucks[1].Id, DriverId=drivers[1].Id,
            FromLocation="Pune, Maharashtra", ToLocation="Hyderabad, Telangana",
            StartDate=DateTime.Today.AddDays(-3), DistanceKm=560,
            Revenue=38500, CargoDescription="Auto Parts", Status=TripStatus.InProgress
        };
        db.Trips.AddRange(trip1, trip2);
        await db.SaveChangesAsync();

        // Expenses
        db.Expenses.AddRange(
            new Expense { TenantId=tenant1.Id, TripId=trip1.Id, Category=ExpenseCategory.Fuel,
                Amount=8500, ExpenseDate=trip1.StartDate.Date, Description="Diesel 100L @₹85", HasReceipt=true },
            new Expense { TenantId=tenant1.Id, TripId=trip1.Id, Category=ExpenseCategory.Toll,
                Amount=1200, ExpenseDate=trip1.StartDate.Date, Description="NH48 Toll Booth" },
            new Expense { TenantId=tenant1.Id, TripId=trip1.Id, Category=ExpenseCategory.Meal,
                Amount=650, ExpenseDate=trip1.StartDate.AddDays(1).Date, Description="Driver Meal" },
            new Expense { TenantId=tenant1.Id, TripId=trip1.Id, Category=ExpenseCategory.Wages,
                Amount=8150, ExpenseDate=trip1.EndDate!.Value.Date, Description="Driver Wages" }
        );

        // Alerts
        db.Alerts.AddRange(
            new Alert { TenantId=tenant1.Id, Type=AlertType.TruckInsuranceExpiry,
                Severity=AlertSeverity.Critical, Title="Insurance Expiry — MH12AB1234",
                Message="Vehicle insurance expires in 5 days",
                ReferenceId=trucks[0].Id, ReferenceType="Truck",
                ExpiryDate=DateTime.Today.AddDays(5), DaysRemaining=5 },
            new Alert { TenantId=tenant1.Id, Type=AlertType.DriverLicenseExpiry,
                Severity=AlertSeverity.Critical, Title="License Expired — Suresh Patel",
                Message="Driver license has expired",
                ReferenceId=drivers[1].Id, ReferenceType="Driver",
                ExpiryDate=DateTime.Today.AddDays(-1), DaysRemaining=-1 },
            new Alert { TenantId=tenant1.Id, Type=AlertType.DriverLicenseExpiry,
                Severity=AlertSeverity.Warning, Title="License Expiry — Rajesh Kumar",
                Message="Driver license expires in 12 days",
                ReferenceId=drivers[0].Id, ReferenceType="Driver",
                ExpiryDate=DateTime.Today.AddDays(12), DaysRemaining=12 },
            new Alert { TenantId=tenant1.Id, Type=AlertType.TruckFitnessExpiry,
                Severity=AlertSeverity.Warning, Title="Fitness Expiry — MH12AB1234",
                Message="Fitness certificate expires in 15 days",
                ReferenceId=trucks[0].Id, ReferenceType="Truck",
                ExpiryDate=DateTime.Today.AddDays(15), DaysRemaining=15 }
        );

        await db.SaveChangesAsync();
    }

    public static async Task SeedMenuItemsAsync(AppDbContext db)
    {
        if (await db.MenuItems.AnyAsync()) return;

        // Top-level groups / leaf items
        var dashboard = new MenuItem { Title = "Dashboard", Icon = "fas fa-tachometer-alt", Controller = "Dashboard", Action = "Index", SortOrder = 10, IsActive = true };
        var companies = new MenuItem { Title = "Companies",  Icon = "fas fa-building",       SortOrder = 20, SuperAdminOnly = true,   IsActive = true };
        var roles     = new MenuItem { Title = "Roles",      Icon = "fas fa-shield-alt",     SortOrder = 30, SuperAdminOnly = true,   IsActive = true };
        var users     = new MenuItem { Title = "Users",      Icon = "fas fa-users",          SortOrder = 40, TenantAdminOrAbove = true, RequiredPermission = "users.view",    IsActive = true };
        var trucks    = new MenuItem { Title = "Trucks",     Icon = "fas fa-truck",          SortOrder = 50, RequiredPermission = "trucks.view",   IsActive = true };
        var drivers   = new MenuItem { Title = "Drivers",    Icon = "fas fa-user-tie",       SortOrder = 60, RequiredPermission = "drivers.view",  IsActive = true };
        var trips     = new MenuItem { Title = "Trips",      Icon = "fas fa-route",          SortOrder = 70, RequiredPermission = "trips.view",    IsActive = true };
        var expenses  = new MenuItem { Title = "Expenses",   Icon = "fas fa-receipt",        SortOrder = 80, RequiredPermission = "expenses.view", IsActive = true };
        var reports   = new MenuItem { Title = "Reports",    Icon = "fas fa-chart-line",     SortOrder = 90, RequiredPermission = "reports.view",  IsActive = true };
        var alerts    = new MenuItem { Title = "Alerts",     Icon = "fas fa-bell",           Controller = "Alert",  Action = "Index", SortOrder = 100, RequiredPermission = "alerts.view", IsActive = true };

        db.MenuItems.AddRange(dashboard, companies, roles, users, trucks, drivers, trips, expenses, reports, alerts);
        await db.SaveChangesAsync();

        // Children
        db.MenuItems.AddRange(
            // Companies
            new MenuItem { Title = "Company List", Icon = "far fa-circle", Controller = "Tenant", Action = "Index",  ParentId = companies.Id, SortOrder = 1, IsActive = true },
            new MenuItem { Title = "Add Company",  Icon = "far fa-circle", Controller = "Tenant", Action = "Create", ParentId = companies.Id, SortOrder = 2, IsActive = true },
            // Roles
            new MenuItem { Title = "Role List",    Icon = "far fa-circle", Controller = "Role",   Action = "Index",  ParentId = roles.Id,     SortOrder = 1, IsActive = true },
            // Users
            new MenuItem { Title = "User List",    Icon = "far fa-circle", Controller = "User",   Action = "Index",  ParentId = users.Id,     SortOrder = 1, IsActive = true },
            new MenuItem { Title = "Add User",     Icon = "far fa-circle", Controller = "User",   Action = "Create", ParentId = users.Id,     SortOrder = 2, RequiredPermission = "users.create",    IsActive = true },
            // Trucks
            new MenuItem { Title = "Truck List",   Icon = "far fa-circle", Controller = "Truck",  Action = "Index",  ParentId = trucks.Id,    SortOrder = 1, IsActive = true },
            new MenuItem { Title = "Add Truck",    Icon = "far fa-circle", Controller = "Truck",  Action = "Create", ParentId = trucks.Id,    SortOrder = 2, RequiredPermission = "trucks.create",   IsActive = true },
            // Drivers
            new MenuItem { Title = "Driver List",  Icon = "far fa-circle", Controller = "Driver", Action = "Index",  ParentId = drivers.Id,   SortOrder = 1, IsActive = true },
            new MenuItem { Title = "Add Driver",   Icon = "far fa-circle", Controller = "Driver", Action = "Create", ParentId = drivers.Id,   SortOrder = 2, RequiredPermission = "drivers.create",  IsActive = true },
            // Trips
            new MenuItem { Title = "Trip List",    Icon = "far fa-circle", Controller = "Trip",   Action = "Index",  ParentId = trips.Id,     SortOrder = 1, IsActive = true },
            new MenuItem { Title = "New Trip",     Icon = "far fa-circle", Controller = "Trip",   Action = "Create", ParentId = trips.Id,     SortOrder = 2, RequiredPermission = "trips.create",    IsActive = true },
            // Expenses
            new MenuItem { Title = "Expense List", Icon = "far fa-circle", Controller = "Expense",Action = "Index",  ParentId = expenses.Id,  SortOrder = 1, IsActive = true },
            new MenuItem { Title = "Add Expense",  Icon = "far fa-circle", Controller = "Expense",Action = "Create", ParentId = expenses.Id,  SortOrder = 2, RequiredPermission = "expenses.create", IsActive = true },
            // Reports
            new MenuItem { Title = "P&L Report",          Icon = "far fa-circle", Controller = "Report", Action = "Index",            ParentId = reports.Id, SortOrder = 1, IsActive = true },
            new MenuItem { Title = "Export Trips (Excel)", Icon = "far fa-circle", Controller = "Report", Action = "ExportTripsExcel", ParentId = reports.Id, SortOrder = 2, RequiredPermission = "reports.export", IsActive = true }
        );
        await db.SaveChangesAsync();
    }
}
