using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetPro.Models.Entities;

// ═══════════════════════════════════════════════════
//  TRUCK
// ═══════════════════════════════════════════════════
public class Truck : TenantBaseEntity
{
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

    // Navigation
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

public enum TruckStatus { Active = 1, InMaintenance = 2, Inactive = 3 }

// ═══════════════════════════════════════════════════
//  DRIVER
// ═══════════════════════════════════════════════════
public class Driver : TenantBaseEntity
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [Required, MaxLength(30)]
    public string LicenseNumber { get; set; } = string.Empty;

    public DateTime? LicenseExpiry { get; set; }

    [MaxLength(50)]
    public string? LicenseType { get; set; }  // HMV, LMV, etc.

    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? AadharNumber { get; set; }

    [MaxLength(30)]
    public string? PanNumber { get; set; }

    public DriverStatus Status { get; set; } = DriverStatus.Active;
    public decimal? MonthlySalary { get; set; }

    [MaxLength(200)]
    public string? BankAccountNumber { get; set; }

    [MaxLength(20)]
    public string? IFSC { get; set; }

    public string? ProfileImage { get; set; }

    // Navigation
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

public enum DriverStatus { Active = 1, OnLeave = 2, Inactive = 3 }

// ═══════════════════════════════════════════════════
//  TRIP
// ═══════════════════════════════════════════════════
public class Trip : TenantBaseEntity
{
    [Required, MaxLength(20)]
    public string TripNumber { get; set; } = string.Empty;   // T-1045

    public int TruckId { get; set; }
    public Truck Truck { get; set; } = null!;

    public int DriverId { get; set; }
    public Driver Driver { get; set; } = null!;

    [Required, MaxLength(200)]
    public string FromLocation { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ToLocation { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public decimal? DistanceKm { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Revenue { get; set; }

    [MaxLength(200)]
    public string? CargoDescription { get; set; }

    public decimal? CargoWeightTons { get; set; }

    [MaxLength(200)]
    public string? ClientName { get; set; }

    [MaxLength(100)]
    public string? LRNumber { get; set; }   // Lorry Receipt

    public TripStatus Status { get; set; } = TripStatus.Scheduled;

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<TripDocument> Documents { get; set; } = new List<TripDocument>();

    // Computed (not mapped)
    [NotMapped]
    public decimal TotalExpenses => Expenses?.Where(e => !e.IsDeleted).Sum(e => e.Amount) ?? 0;

    [NotMapped]
    public decimal NetProfit => Revenue - TotalExpenses;
}

public enum TripStatus { Scheduled = 1, InProgress = 2, Completed = 3, Cancelled = 4 }

// ═══════════════════════════════════════════════════
//  TRIP DOCUMENT
// ═══════════════════════════════════════════════════
public class TripDocument : TenantBaseEntity
{
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    [Required, MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [MaxLength(100)]
    public string? DocumentType { get; set; }   // LR, Invoice, Bill, etc.
}

// ═══════════════════════════════════════════════════
//  EXPENSE
// ═══════════════════════════════════════════════════
public class Expense : TenantBaseEntity
{
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public ExpenseCategory Category { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? VendorName { get; set; }

    [MaxLength(100)]
    public string? BillNumber { get; set; }

    public string? ReceiptPath { get; set; }    // uploaded bill image/PDF
    public bool HasReceipt { get; set; } = false;
}

public enum ExpenseCategory
{
    Fuel = 1,
    Maintenance = 2,
    Toll = 3,
    Service = 4,
    Meal = 5,
    Wages = 6,
    Tyre = 7,
    Permit = 8,
    Other = 9
}

// ═══════════════════════════════════════════════════
//  ALERT
// ═══════════════════════════════════════════════════
public class Alert : TenantBaseEntity
{
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Message { get; set; }

    public int? ReferenceId { get; set; }       // TruckId or DriverId
    public string? ReferenceType { get; set; }  // "Truck" or "Driver"

    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }

    public bool IsRead { get; set; } = false;
    public bool IsNotified { get; set; } = false;
    public DateTime? NotifiedAt { get; set; }
}

public enum AlertType
{
    DriverLicenseExpiry = 1,
    TruckFitnessExpiry = 2,
    TruckInsuranceExpiry = 3,
    TruckTaxExpiry = 4,
    TruckPermitExpiry = 5
}

public enum AlertSeverity { Critical = 1, Warning = 2, Info = 3 }

// ═══════════════════════════════════════════════════
//  AUDIT LOG
// ═══════════════════════════════════════════════════
public class AuditLog : BaseEntity
{
    public int? TenantId { get; set; }
    public int? UserId { get; set; }

    [MaxLength(100)]
    public string Module { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
