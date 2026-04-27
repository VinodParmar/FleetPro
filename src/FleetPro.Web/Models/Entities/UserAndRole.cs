using System.ComponentModel.DataAnnotations;

namespace FleetPro.Models.Entities;

public class ApplicationUser : BaseEntity
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public int? TenantId { get; set; }   // null = Super Admin
    public Tenant? Tenant { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? LastLoginAt { get; set; }
    public string? ProfileImage { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

public enum UserStatus { Active = 1, Inactive = 2, Locked = 3 }

// ─────────────────────────────────────────────────────────────
public class Role : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; } = false;  // SuperAdmin, TenantAdmin etc.
    public int? TenantId { get; set; }               // null = global role
    public Tenant? Tenant { get; set; }

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

// ─────────────────────────────────────────────────────────────
public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

// ─────────────────────────────────────────────────────────────
public class Permission : BaseEntity
{
    [Required, MaxLength(100)]
    public string Module { get; set; } = string.Empty;   // Trucks, Drivers, Trips…

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;   // View, Create, Edit, Delete

    [Required, MaxLength(150)]
    public string Key { get; set; } = string.Empty;      // trucks.view, trucks.create…

    [MaxLength(200)]
    public string? Description { get; set; }

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

// ─────────────────────────────────────────────────────────────
public class RolePermission : BaseEntity
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    public bool IsGranted { get; set; } = true;
}

// ─────────────────────────────────────────────────────────────
public class MenuItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;       // Also used as L10n key
    public string? Icon { get; set; }                        // e.g. "fas fa-truck"
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public int? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    public int SortOrder { get; set; }
    public string? RequiredPermission { get; set; }          // e.g. "trucks.view"
    public bool SuperAdminOnly { get; set; }
    public bool TenantAdminOrAbove { get; set; }
    public bool IsActive { get; set; } = true;
}

// ─────────────────────────────────────────────────────────────
public class UserPermission : BaseEntity
{
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    public bool IsGranted { get; set; } = true;  // Override: can deny role permission too
}
