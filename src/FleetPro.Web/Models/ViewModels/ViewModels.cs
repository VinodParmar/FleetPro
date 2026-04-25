using System.ComponentModel.DataAnnotations;
using FleetPro.Models.Entities;

namespace FleetPro.Models.ViewModels;

// ── AUTH ──────────────────────────────────────────────────────
public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

// ── TENANT ────────────────────────────────────────────────────
public class TenantViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Company name is required")]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subdomain is required")]
    [MaxLength(100), RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Only lowercase letters, numbers and hyphens")]
    public string Subdomain { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ContactPerson { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(30)]
    public string? GstNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    public TenantPlan Plan { get; set; } = TenantPlan.Starter;
    public TenantStatus Status { get; set; } = TenantStatus.Trial;

    public int MaxTrucks { get; set; } = 10;
    public int MaxUsers { get; set; } = 5;

    // Owner details (for Create only)
    [Required(ErrorMessage = "Owner name is required")]
    public string OwnerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Owner email is required"), EmailAddress]
    public string OwnerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required"), MinLength(8)]
    [DataType(DataType.Password)]
    public string OwnerPassword { get; set; } = string.Empty;

    public string? OwnerPhone { get; set; }

    // Stats (read only)
    public int TruckCount { get; set; }
    public int DriverCount { get; set; }
    public int UserCount { get; set; }
}

// ── USER ──────────────────────────────────────────────────────
public class UserViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public int? TenantId { get; set; }

    [Required]
    public int RoleId { get; set; }

    [DataType(DataType.Password), MinLength(8)]
    public string? Password { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    public string? CurrentRole { get; set; }
}

public class UserPermissionViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<PermissionGroupViewModel> Groups { get; set; } = new();
}

public class PermissionGroupViewModel
{
    public string Module { get; set; } = string.Empty;
    public List<PermissionItemViewModel> Permissions { get; set; } = new();
}

public class PermissionItemViewModel
{
    public int PermissionId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsGrantedByRole { get; set; }
    public bool? IsOverriddenByUser { get; set; }  // null = no override
}

// ── TRUCK ─────────────────────────────────────────────────────
public class TruckViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string NumberPlate { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Make { get; set; }

    public int? ManufacturingYear { get; set; }

    [MaxLength(50)]
    public string? EngineNumber { get; set; }

    [MaxLength(50)]
    public string? ChassisNumber { get; set; }

    public DateTime? FitnessExpiry { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    public DateTime? TaxExpiry { get; set; }
    public DateTime? PermitExpiry { get; set; }

    [MaxLength(50)]
    public string? InsurancePolicyNumber { get; set; }

    public TruckStatus Status { get; set; } = TruckStatus.Active;
    public decimal? LoadCapacityTons { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
}

// ── DRIVER ────────────────────────────────────────────────────
public class DriverViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress, MaxLength(150)]
    public string? Email { get; set; }

    [Required, MaxLength(30)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required]
    public DateTime? LicenseExpiry { get; set; }

    [MaxLength(50)]
    public string? LicenseType { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? AadharNumber { get; set; }

    [MaxLength(30)]
    public string? PanNumber { get; set; }

    public DriverStatus Status { get; set; } = DriverStatus.Active;
    public decimal? MonthlySalary { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? IFSC { get; set; }

    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public int TripCount { get; set; }
}

// ── TRIP ──────────────────────────────────────────────────────
public class TripViewModel
{
    public int Id { get; set; }
    public string TripNumber { get; set; } = string.Empty;

    [Required]
    public int TruckId { get; set; }

    [Required]
    public int DriverId { get; set; }

    [Required, MaxLength(200)]
    public string FromLocation { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ToLocation { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime? EndDate { get; set; }
    public decimal? DistanceKm { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal Revenue { get; set; }

    [MaxLength(200)]
    public string? CargoDescription { get; set; }

    public decimal? CargoWeightTons { get; set; }

    [MaxLength(200)]
    public string? ClientName { get; set; }

    [MaxLength(100)]
    public string? LRNumber { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Scheduled;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? TenantId { get; set; }

    // Display
    public string? TruckPlate { get; set; }
    public string? DriverName { get; set; }
    public string? TenantName { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
}

// ── EXPENSE ───────────────────────────────────────────────────
public class ExpenseViewModel
{
    public int Id { get; set; }

    [Required]
    public int TripId { get; set; }

    [Required]
    public ExpenseCategory Category { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? VendorName { get; set; }

    [MaxLength(100)]
    public string? BillNumber { get; set; }

    public IFormFile? Receipt { get; set; }
    public string? ExistingReceiptPath { get; set; }

    // Display
    public string? TripNumber { get; set; }
    public string? TripRoute { get; set; }
}

// ── REPORT ────────────────────────────────────────────────────
public class ReportFilterViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? TenantId { get; set; }
    public int? TruckId { get; set; }
    public int? DriverId { get; set; }
    public TripStatus? TripStatus { get; set; }
    public string ReportType { get; set; } = "Trips";
}

public class PLReportViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal TripRevenue { get; set; }
    public decimal OtherRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal FuelExpenses { get; set; }
    public decimal MaintenanceExpenses { get; set; }
    public decimal TollExpenses { get; set; }
    public decimal WagesExpenses { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal NetProfit => TotalRevenue - TotalExpenses;
    public decimal ProfitMarginPct => TotalRevenue > 0 ? Math.Round(NetProfit / TotalRevenue * 100, 2) : 0;
    public List<TripPLViewModel> TripBreakdown { get; set; } = [];
}

public class TripPLViewModel
{
    public string TripNumber { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit => Revenue - Expenses;
    public decimal MarginPct => Revenue > 0 ? Math.Round(Profit / Revenue * 100, 2) : 0;
}
