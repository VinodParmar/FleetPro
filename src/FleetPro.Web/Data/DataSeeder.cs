using BCrypt.Net;
using FleetPro.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FleetPro.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        Log.Information("Starting database seeding...");

        // Just ensure we can connect - tables should be created by SQL script
        var canConnect = await db.Database.CanConnectAsync();
        Log.Information("Can connect to database: {CanConnect}", canConnect);

        if (!canConnect)
        {
            Log.Error("Cannot connect to database! Please run the SQL script first.");
            throw new Exception("Database not found. Run database\\FleetPro_Schema.sql first.");
        }

        Log.Information("Seeding Permissions...");
        await SeedPermissionsAsync(db);

        Log.Information("Seeding Roles...");
        await SeedRolePermissionsAsync(db);

        Log.Information("Seeding SuperAdmin...");
        await SeedSuperAdminAsync(db);

        Log.Information("Seeding ExpenseCategories...");
        await SeedExpenseCategoriesAsync(db);

        Log.Information("Seeding Sample Tenants...");
        await SeedSampleTenantAsync(db);

        Log.Information("Seeding MenuItems...");
        await SeedMenuItemsAsync(db);

        Log.Information("Database seeding completed!");
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
            ("Agents",   new[] { "View","Create","Edit","Delete" }),
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
            .Select(rp => new { rp.Id, rp.RoleId, rp.PermissionId })
            .ToListAsync();
        var existing = existingRpList
            .Select(rp => (rp.RoleId, rp.PermissionId))
            .ToHashSet();

        // Clean up any existing duplicates in DB (client-side grouping)
        var duplicateGroups = existingRpList
            .GroupBy(rp => new { rp.RoleId, rp.PermissionId })
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Any())
        {
            var idsToRemove = duplicateGroups
                .SelectMany(g => g.OrderBy(rp => rp.Id).Skip(1).Select(rp => rp.Id))
                .ToList();

            var toRemove = await db.RolePermissions
                .Where(rp => idsToRemove.Contains(rp.Id))
                .ToListAsync();

            db.RolePermissions.RemoveRange(toRemove);
            await db.SaveChangesAsync();
        }

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
        var exists = await db.Users.AnyAsync(u => u.Email == "vinod@fleetpro.in");
        Log.Information("SuperAdmin exists: {Exists}", exists);

        if (exists) return;

        var superAdmin = new ApplicationUser
        {
            FullName     = "Vinod Parmar",
            Email        = "vinod@fleetpro.in",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2025!"),
            Phone        = "+91 98765 00001",
            Status       = UserStatus.Active
        };
        db.Users.Add(superAdmin);
        await db.SaveChangesAsync();
        Log.Information("Created SuperAdmin user with Id: {Id}", superAdmin.Id);

        var superAdminRole = await db.Roles.FirstAsync(r => r.Name == "SuperAdmin");
        db.UserRoles.Add(new UserRole { UserId=superAdmin.Id, RoleId=superAdminRole.Id });
        await db.SaveChangesAsync();
        Log.Information("Assigned SuperAdmin role to user");
    }

    private static async Task SeedSampleTenantAsync(AppDbContext db)
    {
        var tenantExists = await db.Tenants.AnyAsync();
        Log.Information("Tenants exist: {Exists}", tenantExists);

        if (tenantExists) return;

        // Tenant 1: JPL Service - Owner: Narendra Parmar
        var tenant1 = new Tenant
        {
            CompanyName="JPL Service", Subdomain="jpl-service",
            ContactPerson="Narendra Parmar", Email="narendra@jplservice.in",
            Phone="+91 98765 00002", 
            Address="Industrial Area, Sector 5", City="Gurgaon", State="Haryana",
            Plan=TenantPlan.Business, Status=TenantStatus.Active,
            MaxTrucks=50, MaxUsers=10,
            SubscriptionStartDate=DateTime.UtcNow.AddMonths(-3),
            SubscriptionEndDate=DateTime.UtcNow.AddMonths(9)
        };

        // Tenant 2: M/S Parmar Travels and Services - Owner: Sunil Jaat
        var tenant2 = new Tenant
        {
            CompanyName="M/S Parmar Travels and Services", Subdomain="parmar-travels",
            ContactPerson="Sunil Jaat", Email="sunil@parmartravels.in",
            Phone="+91 98765 00003",
            Address="Transport Nagar", City="Delhi", State="Delhi",
            Plan=TenantPlan.Premium, Status=TenantStatus.Active,
            MaxTrucks=100, MaxUsers=20,
            SubscriptionStartDate=DateTime.UtcNow.AddMonths(-6),
            SubscriptionEndDate=DateTime.UtcNow.AddMonths(6)
        };

        // Tenant 3: NVH Enterprises - Owner: Virendra Parmar
        var tenant3 = new Tenant
        {
            CompanyName="NVH Enterprises", Subdomain="nvh-enterprises",
            ContactPerson="Virendra Parmar", Email="virendra@nvhenterprises.in",
            Phone="+91 98765 00004",
            Address="Logistics Hub", City="Jaipur", State="Rajasthan",
            Plan=TenantPlan.Business, Status=TenantStatus.Active,
            MaxTrucks=30, MaxUsers=8,
            SubscriptionStartDate=DateTime.UtcNow.AddMonths(-2),
            SubscriptionEndDate=DateTime.UtcNow.AddMonths(10)
        };

        db.Tenants.AddRange(tenant1, tenant2, tenant3);
        await db.SaveChangesAsync();

        var tenantAdminRole = await db.Roles.FirstAsync(r => r.Name == "TenantAdmin");
        var dataEntryRole   = await db.Roles.FirstAsync(r => r.Name == "DataEntryOperator");

        // Tenant 1 Owner: Narendra Parmar
        var t1Owner = new ApplicationUser
        {
            FullName="Narendra Parmar", Email="narendra@jplservice.in",
            PasswordHash=BCrypt.Net.BCrypt.HashPassword("Narendra@2025!"),
            Phone="+91 98765 00002", TenantId=tenant1.Id, Status=UserStatus.Active
        };

        // Tenant 2 Owner: Sunil Jaat
        var t2Owner = new ApplicationUser
        {
            FullName="Sunil Jaat", Email="sunil@parmartravels.in",
            PasswordHash=BCrypt.Net.BCrypt.HashPassword("Sunil@2025!"),
            Phone="+91 98765 00003", TenantId=tenant2.Id, Status=UserStatus.Active
        };

        // Tenant 2 Data Entry: Jitendra Parmar
        var t2DataEntry = new ApplicationUser
        {
            FullName="Jitendra Parmar", Email="jitendra@parmartravels.in",
            PasswordHash=BCrypt.Net.BCrypt.HashPassword("Jitendra@2025!"),
            Phone="+91 98765 00005", TenantId=tenant2.Id, Status=UserStatus.Active
        };

        // Tenant 3 Owner: Virendra Parmar
        var t3Owner = new ApplicationUser
        {
            FullName="Virendra Parmar", Email="virendra@nvhenterprises.in",
            PasswordHash=BCrypt.Net.BCrypt.HashPassword("Virendra@2025!"),
            Phone="+91 98765 00004", TenantId=tenant3.Id, Status=UserStatus.Active
        };

        db.Users.AddRange(t1Owner, t2Owner, t2DataEntry, t3Owner);
        await db.SaveChangesAsync();

        db.UserRoles.AddRange(
            new UserRole { UserId=t1Owner.Id,    RoleId=tenantAdminRole.Id },
            new UserRole { UserId=t2Owner.Id,    RoleId=tenantAdminRole.Id },
            new UserRole { UserId=t2DataEntry.Id, RoleId=dataEntryRole.Id },
            new UserRole { UserId=t3Owner.Id,    RoleId=tenantAdminRole.Id }
        );

        // Sample Trucks for Tenant 2 (M/S Parmar Travels)
        var trucks = new List<Truck>
        {
            new() { TenantId=tenant2.Id, NumberPlate="DL01AB1234", Model="Tata Prima 4928S",
                    Make="Tata", ManufacturingYear=2022,
                    FitnessExpiry=DateTime.Today.AddDays(45), InsuranceExpiry=DateTime.Today.AddDays(30),
                    Status=TruckStatus.Active, LoadCapacityTons=25 },
            new() { TenantId=tenant2.Id, NumberPlate="DL01CD5678", Model="Ashok Leyland 2518",
                    Make="Ashok Leyland", ManufacturingYear=2021,
                    FitnessExpiry=DateTime.Today.AddMonths(3), InsuranceExpiry=DateTime.Today.AddMonths(4),
                    Status=TruckStatus.Active, LoadCapacityTons=18 },
            new() { TenantId=tenant2.Id, NumberPlate="DL01EF9012", Model="BharatBenz 3523R",
                    Make="BharatBenz", ManufacturingYear=2023,
                    FitnessExpiry=DateTime.Today.AddMonths(10), InsuranceExpiry=DateTime.Today.AddMonths(8),
                    Status=TruckStatus.Active, LoadCapacityTons=35 },
            new() { TenantId=tenant1.Id, NumberPlate="HR55GH3456", Model="Eicher Pro 3019",
                    Make="Eicher", ManufacturingYear=2020,
                    FitnessExpiry=DateTime.Today.AddDays(15), InsuranceExpiry=DateTime.Today.AddDays(5),
                    Status=TruckStatus.Active, LoadCapacityTons=19 },
            new() { TenantId=tenant3.Id, NumberPlate="RJ14JK7890", Model="Mahindra Blazo X 35",
                    Make="Mahindra", ManufacturingYear=2022,
                    FitnessExpiry=DateTime.Today.AddDays(2), InsuranceExpiry=DateTime.Today.AddMonths(7),
                    Status=TruckStatus.InMaintenance, LoadCapacityTons=35 },
        };
        db.Trucks.AddRange(trucks);

        // Sample Drivers
        var drivers = new List<Driver>
        {
            new() { TenantId=tenant2.Id, FullName="Rajesh Kumar", Phone="+91 98765 11111",
                    LicenseNumber="DL0120200012345", LicenseExpiry=DateTime.Today.AddDays(60),
                    LicenseType="HMV", Status=DriverStatus.Active, MonthlySalary=28000 },
            new() { TenantId=tenant2.Id, FullName="Mohan Singh", Phone="+91 98765 22222",
                    LicenseNumber="DL0120190054321", LicenseExpiry=DateTime.Today.AddDays(-1),
                    LicenseType="HMV", Status=DriverStatus.Active, MonthlySalary=25000 },
            new() { TenantId=tenant2.Id, FullName="Ramesh Yadav", Phone="+91 98765 33333",
                    LicenseNumber="DL0120210067890", LicenseExpiry=DateTime.Today.AddMonths(12),
                    LicenseType="HMV", Status=DriverStatus.Active, MonthlySalary=30000 },
            new() { TenantId=tenant1.Id, FullName="Suresh Sharma", Phone="+91 98765 44444",
                    LicenseNumber="HR0519950012345", LicenseExpiry=DateTime.Today.AddDays(12),
                    LicenseType="HMV", Status=DriverStatus.Active, MonthlySalary=26000 },
            new() { TenantId=tenant3.Id, FullName="Amit Meena", Phone="+91 98765 55555",
                    LicenseNumber="RJ1420000067890", LicenseExpiry=DateTime.Today.AddMonths(8),
                    LicenseType="HMV", Status=DriverStatus.Active, MonthlySalary=24000 },
        };
        db.Drivers.AddRange(drivers);

        // Sample Agents (Brokers)
        var agents = new List<Agent>
        {
            new() { TenantId=tenant2.Id, Name="ABC Logistics", Phone="+91 99887 76655",
                    CompanyName="ABC Logistics Pvt Ltd", GSTNumber="07AABCU9603R1ZM", Status=AgentStatus.Active },
            new() { TenantId=tenant2.Id, Name="XYZ Transport", Phone="+91 88776 65544",
                    CompanyName="XYZ Transport Co.", GSTNumber="07AADCX1234M1ZK", Status=AgentStatus.Active },
            new() { TenantId=tenant2.Id, Name="Delhi Freight Services", Phone="+91 77665 54433",
                    CompanyName="Delhi Freight Services", GSTNumber="07AABCD5678M1ZP", Status=AgentStatus.Active },
            new() { TenantId=tenant1.Id, Name="Haryana Cargo", Phone="+91 66554 43322",
                    CompanyName="Haryana Cargo Pvt Ltd", GSTNumber="06AABCH1234M1ZK", Status=AgentStatus.Active },
            new() { TenantId=tenant3.Id, Name="Rajasthan Logistics", Phone="+91 55443 32211",
                    CompanyName="Rajasthan Logistics Co.", GSTNumber="08AABCR5678M1ZP", Status=AgentStatus.Active },
        };
        db.Agents.AddRange(agents);
        await db.SaveChangesAsync();

        // Sample Trips for Tenant 2 (M/S Parmar Travels)
        var trip1 = new Trip
        {
            TenantId=tenant2.Id, TripNumber="T-0001", TruckId=trucks[0].Id, DriverId=drivers[0].Id,
            Status=TripStatus.Completed
        };
        var trip2 = new Trip
        {
            TenantId=tenant2.Id, TripNumber="T-0002", TruckId=trucks[1].Id, DriverId=drivers[1].Id,
            Status=TripStatus.InProgress
        };
        var trip3 = new Trip
        {
            TenantId=tenant2.Id, TripNumber="T-0003", TruckId=trucks[2].Id, DriverId=drivers[2].Id,
            Status=TripStatus.Scheduled
        };
        db.Trips.AddRange(trip1, trip2, trip3);
        await db.SaveChangesAsync();

        // Trip Phases with Rate and DealAmount
        var phases = new[]
        {
            // Trip 1 - UP Phase (Completed)
            new TripPhase { TenantId=tenant2.Id, TripId=trip1.Id, PhaseType=TripPhaseType.Up,
                FromLocation="Delhi, NCR", ToLocation="Mumbai, Maharashtra",
                StartDate=DateTime.Today.AddDays(-10), EndDate=DateTime.Today.AddDays(-8),
                StartMeterReading=50000, EndMeterReading=51450,
                AgentId=agents[0].Id, LRNumber="LR-2025-001", CargoDescription="FMCG Goods",
                NetWeight=22.5m, Rate=2000, DealAmount=45000, // 22.5 tons × ₹2000
                Status=TripPhaseStatus.Completed },
            // Trip 1 - DOWN Phase (Completed)
            new TripPhase { TenantId=tenant2.Id, TripId=trip1.Id, PhaseType=TripPhaseType.Down,
                FromLocation="Mumbai, Maharashtra", ToLocation="Delhi, NCR",
                StartDate=DateTime.Today.AddDays(-8), EndDate=DateTime.Today.AddDays(-6),
                StartMeterReading=51450, EndMeterReading=52900,
                AgentId=agents[1].Id, LRNumber="LR-2025-002", CargoDescription="Auto Parts",
                NetWeight=18m, Rate=1800, DealAmount=32400, // 18 tons × ₹1800
                Status=TripPhaseStatus.Completed },
            // Trip 2 - UP Phase (In Progress)
            new TripPhase { TenantId=tenant2.Id, TripId=trip2.Id, PhaseType=TripPhaseType.Up,
                FromLocation="Delhi, NCR", ToLocation="Bangalore, Karnataka",
                StartDate=DateTime.Today.AddDays(-2),
                StartMeterReading=25000, EndMeterReading=27100,
                AgentId=agents[2].Id, LRNumber="LR-2025-003", CargoDescription="Electronics",
                NetWeight=20m, Rate=1925, DealAmount=38500, // 20 tons × ₹1925
                Status=TripPhaseStatus.InProgress },
            // Trip 3 - UP Phase (Scheduled)
            new TripPhase { TenantId=tenant2.Id, TripId=trip3.Id, PhaseType=TripPhaseType.Up,
                FromLocation="Delhi, NCR", ToLocation="Chennai, Tamil Nadu",
                StartDate=DateTime.Today.AddDays(2),
                StartMeterReading=30000,
                AgentId=agents[0].Id, LRNumber="LR-2025-004", CargoDescription="Machinery",
                NetWeight=30m, Rate=1750, DealAmount=52500, // 30 tons × ₹1750
                Status=TripPhaseStatus.Pending }
        };
        db.TripPhases.AddRange(phases);
        await db.SaveChangesAsync();

        // Trip Payments
        db.TripPayments.AddRange(
            new TripPayment { TenantId=tenant2.Id, TripId=trip1.Id, PaymentType=PaymentType.Received,
                Amount=40000, PaymentDate=DateTime.Today.AddDays(-9), PaymentMode=PaymentMode.BankTransfer,
                PayerPayee="ABC Logistics", Description="Advance for UP trip", ReferenceNumber="UTR001234" },
            new TripPayment { TenantId=tenant2.Id, TripId=trip1.Id, PaymentType=PaymentType.Received,
                Amount=35000, PaymentDate=DateTime.Today.AddDays(-5), PaymentMode=PaymentMode.UPI,
                PayerPayee="XYZ Transport", Description="DOWN trip settlement", ReferenceNumber="UPI78901" },
            new TripPayment { TenantId=tenant2.Id, TripId=trip1.Id, PaymentType=PaymentType.Paid,
                Amount=5000, PaymentDate=DateTime.Today.AddDays(-8), PaymentMode=PaymentMode.Cash,
                PayerPayee="Driver Rajesh", Description="Driver advance" },
            new TripPayment { TenantId=tenant2.Id, TripId=trip2.Id, PaymentType=PaymentType.Received,
                Amount=20000, PaymentDate=DateTime.Today.AddDays(-1), PaymentMode=PaymentMode.BankTransfer,
                PayerPayee="Delhi Freight Services", Description="Advance payment", ReferenceNumber="UTR005678" }
        );

        // Expenses
        db.Expenses.AddRange(
            new Expense { TenantId=tenant2.Id, TripId=trip1.Id, Category=ExpenseCategory.Fuel,
                Amount=12500, ExpenseDate=DateTime.Today.AddDays(-10), Description="Diesel 150L @₹83", VendorName="HP Petrol Pump", HasReceipt=true },
            new Expense { TenantId=tenant2.Id, TripId=trip1.Id, Category=ExpenseCategory.Toll,
                Amount=2800, ExpenseDate=DateTime.Today.AddDays(-9), Description="NH48 + NH44 Toll" },
            new Expense { TenantId=tenant2.Id, TripId=trip1.Id, Category=ExpenseCategory.Meal,
                Amount=1500, ExpenseDate=DateTime.Today.AddDays(-9), Description="Driver food allowance" },
            new Expense { TenantId=tenant2.Id, TripId=trip1.Id, Category=ExpenseCategory.Fuel,
                Amount=8500, ExpenseDate=DateTime.Today.AddDays(-7), Description="Diesel 100L @₹85", VendorName="Indian Oil" },
            new Expense { TenantId=tenant2.Id, TripId=trip1.Id, Category=ExpenseCategory.Toll,
                Amount=1800, ExpenseDate=DateTime.Today.AddDays(-7), Description="Return toll" },
            new Expense { TenantId=tenant2.Id, TripId=trip2.Id, Category=ExpenseCategory.Fuel,
                Amount=9500, ExpenseDate=DateTime.Today.AddDays(-2), Description="Diesel 110L @₹86" },
            new Expense { TenantId=tenant2.Id, TripId=trip2.Id, Category=ExpenseCategory.Toll,
                Amount=3200, ExpenseDate=DateTime.Today.AddDays(-1), Description="Delhi-Bangalore toll" }
        );

        // Alerts
        db.Alerts.AddRange(
            new Alert { TenantId=tenant2.Id, Type=AlertType.TruckInsuranceExpiry,
                Severity=AlertSeverity.Warning, Title="Insurance Expiry — DL01AB1234",
                Message="Vehicle insurance expires in 30 days",
                ReferenceId=trucks[0].Id, ReferenceType="Truck",
                ExpiryDate=DateTime.Today.AddDays(30), DaysRemaining=30 },
            new Alert { TenantId=tenant2.Id, Type=AlertType.DriverLicenseExpiry,
                Severity=AlertSeverity.Critical, Title="License Expired — Mohan Singh",
                Message="Driver license has expired",
                ReferenceId=drivers[1].Id, ReferenceType="Driver",
                ExpiryDate=DateTime.Today.AddDays(-1), DaysRemaining=-1 },
            new Alert { TenantId=tenant1.Id, Type=AlertType.TruckInsuranceExpiry,
                Severity=AlertSeverity.Critical, Title="Insurance Expiry — HR55GH3456",
                Message="Vehicle insurance expires in 5 days",
                ReferenceId=trucks[3].Id, ReferenceType="Truck",
                ExpiryDate=DateTime.Today.AddDays(5), DaysRemaining=5 },
            new Alert { TenantId=tenant1.Id, Type=AlertType.DriverLicenseExpiry,
                Severity=AlertSeverity.Warning, Title="License Expiry — Suresh Sharma",
                Message="Driver license expires in 12 days",
                ReferenceId=drivers[3].Id, ReferenceType="Driver",
                ExpiryDate=DateTime.Today.AddDays(12), DaysRemaining=12 },
            new Alert { TenantId=tenant3.Id, Type=AlertType.TruckFitnessExpiry,
                Severity=AlertSeverity.Critical, Title="Fitness Expiry — RJ14JK7890",
                Message="Fitness certificate expires in 2 days",
                ReferenceId=trucks[4].Id, ReferenceType="Truck",
                ExpiryDate=DateTime.Today.AddDays(2), DaysRemaining=2 }
        );

        await db.SaveChangesAsync();
    }

    public static async Task SeedMenuItemsAsync(AppDbContext db)
    {
        if (await db.MenuItems.AnyAsync()) return;

        // ═══════════════════════════════════════════════════════════════
        // MENU STRUCTURE - Logical Grouping
        // ═══════════════════════════════════════════════════════════════
        // 1. Dashboard (standalone)
        // 2. Operations (Trips, Expenses) - daily work
        // 3. Fleet (Trucks, Drivers) - vehicle management
        // 4. Partners (Agents) - external parties
        // 5. Reports & Analytics
        // 6. Administration (Companies, Users, Roles, Alerts, Audit)
        // ═══════════════════════════════════════════════════════════════

        // ── 1. DASHBOARD ──────────────────────────────────────────────
        var dashboard = new MenuItem { 
            Title = "Dashboard", Icon = "fas fa-tachometer-alt", 
            Controller = "Dashboard", Action = "Index", 
            SortOrder = 10, IsActive = true 
        };

        // ── 2. OPERATIONS ─────────────────────────────────────────────
        var operations = new MenuItem { 
            Title = "Operations", Icon = "fas fa-clipboard-list", 
            SortOrder = 20, IsActive = true,
            RequiredPermission = "trips.view"
        };

        // ── 3. FLEET MANAGEMENT ───────────────────────────────────────
        var fleet = new MenuItem { 
            Title = "Fleet", Icon = "fas fa-truck-moving", 
            SortOrder = 30, IsActive = true,
            RequiredPermission = "trucks.view"
        };

        // ── 4. PARTNERS ───────────────────────────────────────────────
        var partners = new MenuItem { 
            Title = "Partners", Icon = "fas fa-handshake", 
            SortOrder = 40, IsActive = true,
            RequiredPermission = "agents.view"
        };

        // ── 5. REPORTS ────────────────────────────────────────────────
        var reports = new MenuItem { 
            Title = "Reports", Icon = "fas fa-chart-bar", 
            SortOrder = 50, IsActive = true,
            RequiredPermission = "reports.view"
        };

        // ── 6. ALERTS ─────────────────────────────────────────────────
        var alerts = new MenuItem { 
            Title = "Alerts", Icon = "fas fa-bell", 
            Controller = "Alert", Action = "Index",
            SortOrder = 60, IsActive = true,
            RequiredPermission = "alerts.view"
        };

        // ── 7. ADMINISTRATION ─────────────────────────────────────────
        var admin = new MenuItem { 
            Title = "Administration", Icon = "fas fa-cog", 
            SortOrder = 100, IsActive = true,
            TenantAdminOrAbove = true
        };

        db.MenuItems.AddRange(dashboard, operations, fleet, partners, reports, alerts, admin);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════════
        // SUB-MENU ITEMS
        // ═══════════════════════════════════════════════════════════════

        db.MenuItems.AddRange(
            // ── OPERATIONS ────────────────────────────────────────────
            new MenuItem { Title = "Trips", Icon = "fas fa-route", 
                Controller = "Trip", Action = "Index", 
                ParentId = operations.Id, SortOrder = 1, IsActive = true },
            new MenuItem { Title = "New Trip", Icon = "fas fa-plus-circle", 
                Controller = "Trip", Action = "Create", 
                ParentId = operations.Id, SortOrder = 2, IsActive = true,
                RequiredPermission = "trips.create" },
            new MenuItem { Title = "Expenses", Icon = "fas fa-receipt", 
                Controller = "Expense", Action = "Index", 
                ParentId = operations.Id, SortOrder = 3, IsActive = true,
                RequiredPermission = "expenses.view" },
            // Add Expense is handled via modal popup on Expense Index page

            // ── FLEET ─────────────────────────────────────────────────
            new MenuItem { Title = "Trucks", Icon = "fas fa-truck", 
                Controller = "Truck", Action = "Index", 
                ParentId = fleet.Id, SortOrder = 1, IsActive = true },
            // Add Truck is handled via modal popup on Truck Index page
            new MenuItem { Title = "Drivers", Icon = "fas fa-user-tie", 
                Controller = "Driver", Action = "Index", 
                ParentId = fleet.Id, SortOrder = 2, IsActive = true,
                RequiredPermission = "drivers.view" },
            // Add Driver is handled via modal popup on Driver Index page

            // ── PARTNERS ──────────────────────────────────────────────
            new MenuItem { Title = "Agents", Icon = "fas fa-users-cog", 
                Controller = "Agent", Action = "Index", 
                ParentId = partners.Id, SortOrder = 1, IsActive = true },
            // Add Agent is handled via modal popup on Agent Index page

            // ── REPORTS ───────────────────────────────────────────────
            new MenuItem { Title = "P&L Summary", Icon = "fas fa-chart-line", 
                Controller = "Report", Action = "Index", 
                ParentId = reports.Id, SortOrder = 1, IsActive = true },
            new MenuItem { Title = "Trip Report", Icon = "fas fa-file-alt", 
                Controller = "Report", Action = "TripReport", 
                ParentId = reports.Id, SortOrder = 2, IsActive = true },
            new MenuItem { Title = "Export to Excel", Icon = "fas fa-file-excel", 
                Controller = "Report", Action = "ExportTripsExcel", 
                ParentId = reports.Id, SortOrder = 3, IsActive = true,
                RequiredPermission = "reports.export" },

            // ── ADMINISTRATION ────────────────────────────────────────
            // Companies - SuperAdmin only
            new MenuItem { Title = "Companies", Icon = "fas fa-building", 
                Controller = "Tenant", Action = "Index", 
                ParentId = admin.Id, SortOrder = 1, IsActive = true,
                SuperAdminOnly = true },
            new MenuItem { Title = "Add Company", Icon = "fas fa-plus-circle", 
                Controller = "Tenant", Action = "Create", 
                ParentId = admin.Id, SortOrder = 2, IsActive = true,
                SuperAdminOnly = true },
            // Users
            new MenuItem { Title = "Users", Icon = "fas fa-users", 
                Controller = "User", Action = "Index", 
                ParentId = admin.Id, SortOrder = 3, IsActive = true,
                RequiredPermission = "users.view" },
            new MenuItem { Title = "Add User", Icon = "fas fa-user-plus", 
                Controller = "User", Action = "Create", 
                ParentId = admin.Id, SortOrder = 4, IsActive = true,
                RequiredPermission = "users.create" },
            // Roles - SuperAdmin only
            new MenuItem { Title = "Roles & Permissions", Icon = "fas fa-shield-alt", 
                Controller = "Role", Action = "Index", 
                ParentId = admin.Id, SortOrder = 5, IsActive = true,
                SuperAdminOnly = true },
            // Expense Categories - SuperAdmin only
            new MenuItem { Title = "Expense Categories", Icon = "fas fa-tags", 
                Controller = "ExpenseCategory", Action = "Index", 
                ParentId = admin.Id, SortOrder = 6, IsActive = true,
                SuperAdminOnly = true },
            // Audit Logs
            new MenuItem { Title = "Audit Logs", Icon = "fas fa-history", 
                Controller = "Audit", Action = "Index", 
                ParentId = admin.Id, SortOrder = 7, IsActive = true,
                TenantAdminOrAbove = true }
        );

        await db.SaveChangesAsync();
        Log.Information("Menu items seeded with logical grouping");
    }

    // Seed default expense categories (Global - shared by all tenants)
    public static async Task SeedExpenseCategoriesAsync(AppDbContext db)
    {
        // Force insert if table is empty (check raw count, not with EF query filter)
        var count = await db.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT 1 FROM ExpenseCategories WHERE IsDeleted = 0)
            BEGIN
                INSERT INTO ExpenseCategories (Name, Icon, Color, SortOrder, IsActive, CreatedAt, IsDeleted)
                VALUES 
                    ('Fuel / Diesel', 'fas fa-gas-pump', 'danger', 1, 1, GETUTCDATE(), 0),
                    ('Toll', 'fas fa-road', 'warning', 2, 1, GETUTCDATE(), 0),
                    ('Driver Bata', 'fas fa-user', 'info', 3, 1, GETUTCDATE(), 0),
                    ('Loading / Unloading', 'fas fa-boxes', 'primary', 4, 1, GETUTCDATE(), 0),
                    ('RTO / Police', 'fas fa-shield-alt', 'secondary', 5, 1, GETUTCDATE(), 0),
                    ('Repair / Maintenance', 'fas fa-wrench', 'dark', 6, 1, GETUTCDATE(), 0),
                    ('Tyre', 'fas fa-circle', 'secondary', 7, 1, GETUTCDATE(), 0),
                    ('Weighment', 'fas fa-balance-scale', 'info', 8, 1, GETUTCDATE(), 0),
                    ('Parking', 'fas fa-parking', 'primary', 9, 1, GETUTCDATE(), 0),
                    ('Food / Meals', 'fas fa-utensils', 'success', 10, 1, GETUTCDATE(), 0),
                    ('Commission / Agency', 'fas fa-handshake', 'warning', 11, 1, GETUTCDATE(), 0),
                    ('Advance', 'fas fa-money-bill-wave', 'success', 12, 1, GETUTCDATE(), 0),
                    ('Other', 'fas fa-ellipsis-h', 'secondary', 99, 1, GETUTCDATE(), 0)
            END
        ");
    }
}
