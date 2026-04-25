using BCrypt.Net;
using FleetPro.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FleetPro.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();
        await SeedPermissionsAsync(db);
        await SeedRolesAsync(db);
        await SeedSuperAdminAsync(db);
        await SeedSampleTenantAsync(db);
    }

    private static async Task SeedPermissionsAsync(AppDbContext db)
    {
        if (await db.Permissions.AnyAsync()) return;

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

        var permissions = new List<Permission>();
        foreach (var (module, actions) in modules)
            foreach (var action in actions)
                permissions.Add(new Permission
                {
                    Module = module, Action = action,
                    Key = $"{module.ToLower()}.{action.ToLower()}",
                    Description = $"{action} {module}"
                });

        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(AppDbContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        var allPerms = await db.Permissions.ToListAsync();

        var superAdminRole  = new Role { Name="SuperAdmin",         Description="Full system access",                IsSystemRole=true };
        var tenantAdminRole = new Role { Name="TenantAdmin",        Description="Full access within their tenant",  IsSystemRole=true };
        var dataEntryRole   = new Role { Name="DataEntryOperator",  Description="Create and edit, no delete",       IsSystemRole=true };
        var viewerRole      = new Role { Name="Viewer",             Description="Read-only access",                 IsSystemRole=true };

        db.Roles.AddRange(superAdminRole, tenantAdminRole, dataEntryRole, viewerRole);
        await db.SaveChangesAsync();

        var rolePerms = new List<RolePermission>();

        // SuperAdmin — ALL
        foreach (var p in allPerms)
            rolePerms.Add(new RolePermission { RoleId=superAdminRole.Id, PermissionId=p.Id, IsGranted=true });

        // TenantAdmin — all except tenants.create / tenants.delete
        foreach (var p in allPerms.Where(p => !(p.Module=="Tenants" && p.Action is "Create" or "Delete")))
            rolePerms.Add(new RolePermission { RoleId=tenantAdminRole.Id, PermissionId=p.Id, IsGranted=true });

        // DataEntry — View + Create + Edit + Attach
        foreach (var p in allPerms.Where(p => p.Action is "View" or "Create" or "Edit" or "AttachDocument" or "AttachBill"))
            rolePerms.Add(new RolePermission { RoleId=dataEntryRole.Id, PermissionId=p.Id, IsGranted=true });

        // Viewer — View only
        foreach (var p in allPerms.Where(p => p.Action == "View"))
            rolePerms.Add(new RolePermission { RoleId=viewerRole.Id, PermissionId=p.Id, IsGranted=true });

        db.RolePermissions.AddRange(rolePerms);
        await db.SaveChangesAsync();
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
}
