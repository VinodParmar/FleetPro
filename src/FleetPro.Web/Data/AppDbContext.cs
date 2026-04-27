using FleetPro.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FleetPro.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─── DbSets ─────────────────────────────────────────────
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<Truck> Trucks => Set<Truck>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripDocument> TripDocuments => Set<TripDocument>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseCategoryMaster> ExpenseCategories => Set<ExpenseCategoryMaster>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── Tenant ──────────────────────────────────────────
        mb.Entity<Tenant>(e =>
        {
            e.ToTable("Tenants");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Subdomain).IsUnique();
            e.Property(x => x.CompanyName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Subdomain).IsRequired().HasMaxLength(100);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── ApplicationUser ─────────────────────────────────
        mb.Entity<ApplicationUser>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).IsRequired().HasMaxLength(150);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(100);
            e.HasOne(x => x.Tenant).WithMany(t => t.Users)
             .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Role ─────────────────────────────────────────────
        mb.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── UserRole ──────────────────────────────────────────
        mb.Entity<UserRole>(e =>
        {
            e.ToTable("UserRoles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.UserRoles)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles)
             .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Permission ────────────────────────────────────────
        mb.Entity<Permission>(e =>
        {
            e.ToTable("Permissions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Key).IsUnique();
        });

        // ── RolePermission ────────────────────────────────────
        mb.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermissions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.RolePermissions)
             .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission).WithMany(p => p.RolePermissions)
             .HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── UserPermission ────────────────────────────────────
        mb.Entity<UserPermission>(e =>
        {
            e.ToTable("UserPermissions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.PermissionId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.UserPermissions)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission).WithMany(p => p.UserPermissions)
             .HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Truck ─────────────────────────────────────────────
        mb.Entity<Truck>(e =>
        {
            e.ToTable("Trucks");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.NumberPlate }).IsUnique();
            e.Property(x => x.LoadCapacityTons).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Tenant).WithMany(t => t.Trucks)
             .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Driver ────────────────────────────────────────────
        mb.Entity<Driver>(e =>
        {
            e.ToTable("Drivers");
            e.HasKey(x => x.Id);
            e.Property(x => x.MonthlySalary).HasColumnType("decimal(12,2)");
            e.HasOne(x => x.Tenant).WithMany(t => t.Drivers)
             .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Trip ──────────────────────────────────────────────
        mb.Entity<Trip>(e =>
        {
            e.ToTable("Trips");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.TripNumber }).IsUnique();
            e.Property(x => x.Revenue).HasColumnType("decimal(18,2)");
            e.Property(x => x.DistanceKm).HasColumnType("decimal(10,2)");
            e.Property(x => x.CargoWeightTons).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Tenant).WithMany(t => t.Trips)
             .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Truck).WithMany(t => t.Trips)
             .HasForeignKey(x => x.TruckId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Driver).WithMany(d => d.Trips)
             .HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── TripDocument ──────────────────────────────────────
        mb.Entity<TripDocument>(e =>
        {
            e.ToTable("TripDocuments");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Trip).WithMany(t => t.Documents)
             .HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Expense ───────────────────────────────────────────
        mb.Entity<Expense>(e =>
        {
            e.ToTable("Expenses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Trip).WithMany(t => t.Expenses)
             .HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CategoryMaster).WithMany(c => c.Expenses)
             .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── ExpenseCategoryMaster (Global - shared by all tenants) ──
        mb.Entity<ExpenseCategoryMaster>(e =>
        {
            e.ToTable("ExpenseCategories");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Alert ─────────────────────────────────────────────
        mb.Entity<Alert>(e =>
        {
            e.ToTable("Alerts");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Tenant).WithMany()
             .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── AuditLog ──────────────────────────────────────────
        mb.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.Id);
        });

        // ── MenuItem ──────────────────────────────────────────
        mb.Entity<MenuItem>(e =>
        {
            e.ToTable("MenuItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(100);
            e.HasOne(x => x.Parent).WithMany(x => x.Children)
             .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
        });
    }

    // Auto-set timestamps on save
    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(ct);
    }

    private void SetTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
