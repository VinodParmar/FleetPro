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

    // New fields for Authorization and PUC
    public DateTime? AuthorizationExpiry { get; set; }  // National Permit
    public DateTime? PUCExpiry { get; set; }            // Pollution Under Control

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
//  AGENT (Broker/Transport Agent)
// ═══════════════════════════════════════════════════
public class Agent : TenantBaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(30)]
    public string? GSTNumber { get; set; }

    [MaxLength(30)]
    public string? PanNumber { get; set; }

    public AgentStatus Status { get; set; } = AgentStatus.Active;

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation
    public ICollection<TripPhase> TripPhases { get; set; } = [];
}

public enum AgentStatus { Active = 1, Inactive = 2 }

// ═══════════════════════════════════════════════════
//  TRIP (Container for Phases & Payments)
// ═══════════════════════════════════════════════════
public class Trip : TenantBaseEntity
{
    [Required, MaxLength(20)]
    public string TripNumber { get; set; } = string.Empty;   // T-1045

    public int TruckId { get; set; }
    public Truck Truck { get; set; } = null!;

    public int DriverId { get; set; }
    public Driver Driver { get; set; } = null!;

    public TripStatus Status { get; set; } = TripStatus.Scheduled;

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<TripDocument> Documents { get; set; } = new List<TripDocument>();
    public ICollection<TripPhase> Phases { get; set; } = new List<TripPhase>();
    public ICollection<TripPayment> Payments { get; set; } = new List<TripPayment>();

    // ═══ COMPUTED PROPERTIES (from Phases) ═══
    [NotMapped]
    public TripPhase? UpPhase => Phases?.FirstOrDefault(p => p.PhaseType == TripPhaseType.Up && !p.IsDeleted);

    [NotMapped]
    public TripPhase? DownPhase => Phases?.FirstOrDefault(p => p.PhaseType == TripPhaseType.Down && !p.IsDeleted);

    // Route display (from phases)
    [NotMapped]
    public string FromLocation => UpPhase?.FromLocation ?? "";

    [NotMapped]
    public string ToLocation => UpPhase?.ToLocation ?? "";

    [NotMapped]
    public DateTime StartDate => UpPhase?.StartDate ?? CreatedAt;

    [NotMapped]
    public DateTime? EndDate => DownPhase?.EndDate ?? UpPhase?.EndDate;

    [NotMapped]
    public string Route => UpPhase != null 
        ? $"{UpPhase.FromLocation} → {UpPhase.ToLocation}" + (DownPhase != null ? $" → {DownPhase.ToLocation}" : "")
        : "";

    // Distance (sum of all phases)
    [NotMapped]
    public decimal TotalDistance => Phases?.Where(p => !p.IsDeleted).Sum(p => p.CalculatedKm) ?? 0;

    // Deal Amount (sum of all phases)
    [NotMapped]
    public decimal TotalDealAmount => Phases?.Where(p => !p.IsDeleted).Sum(p => p.DealAmount) ?? 0;

    // Weight (sum of all phases)
    [NotMapped]
    public decimal TotalNetWeight => Phases?.Where(p => !p.IsDeleted).Sum(p => p.NetWeight ?? 0) ?? 0;

    // ═══ COMPUTED PROPERTIES (from Payments & Expenses) ═══
    [NotMapped]
    public decimal TotalExpenses => Expenses?.Where(e => !e.IsDeleted).Sum(e => e.Amount) ?? 0;

    [NotMapped]
    public decimal TotalPaymentsIn => Payments?.Where(p => !p.IsDeleted && p.PaymentType == PaymentType.Received).Sum(p => p.Amount) ?? 0;

    [NotMapped]
    public decimal TotalPaymentsOut => Payments?.Where(p => !p.IsDeleted && p.PaymentType == PaymentType.Paid).Sum(p => p.Amount) ?? 0;

    // Profit = Deal Amount - Expenses - Payments Out (to drivers, agents, etc.)
    [NotMapped]
    public decimal NetProfit => TotalDealAmount - TotalExpenses - TotalPaymentsOut;

    // Payment Balance = Received - Paid Out
    [NotMapped]
    public decimal PaymentBalance => TotalPaymentsIn - TotalPaymentsOut;

    // Pending Amount = Deal Amount - Received
    [NotMapped]
    public decimal PendingAmount => TotalDealAmount - TotalPaymentsIn;

    // Display helpers
    [NotMapped]
    public string AgentDisplay => UpPhase?.Agent?.Name ?? "";

    [NotMapped]
    public decimal GrossWeight => (UpPhase?.GrossWeight ?? 0) + (DownPhase?.GrossWeight ?? 0);
}

public enum TripStatus { Scheduled = 1, InProgress = 2, Completed = 3, Cancelled = 4 }

// ═══════════════════════════════════════════════════
//  TRIP PHASE (UP / DOWN)
// ═══════════════════════════════════════════════════
public class TripPhase : TenantBaseEntity
{
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public TripPhaseType PhaseType { get; set; }  // Up or Down

    [Required, MaxLength(200)]
    public string FromLocation { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ToLocation { get; set; } = string.Empty;

    // Meter readings for automatic KM calculation
    [Column(TypeName = "decimal(12,2)")]
    public decimal StartMeterReading { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? EndMeterReading { get; set; }

    // Auto-calculated from meter readings
    [NotMapped]
    public decimal CalculatedKm => EndMeterReading.HasValue ? EndMeterReading.Value - StartMeterReading : 0;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Agent (Broker) - FK to Agent master
    public int? AgentId { get; set; }
    public Agent? Agent { get; set; }

    [MaxLength(100)]
    public string? LRNumber { get; set; }

    [MaxLength(200)]
    public string? CargoDescription { get; set; }

    // Weight fields (per phase)
    [Column(TypeName = "decimal(10,2)")]
    public decimal? TareWeight { get; set; }       // Empty truck weight (tons)

    [Column(TypeName = "decimal(10,2)")]
    public decimal? NetWeight { get; set; }        // Cargo weight (tons)

    [NotMapped]
    public decimal GrossWeight => (TareWeight ?? 0) + (NetWeight ?? 0);

    // ═══ RATE & DEAL AMOUNT ═══
    [Column(TypeName = "decimal(18,2)")]
    public decimal Rate { get; set; }              // Rate per ton (₹/ton)

    // Deal Amount = Rate × NetWeight (auto-calculated, but stored for reporting)
    [Column(TypeName = "decimal(18,2)")]
    public decimal DealAmount { get; set; }

    // Auto-calculate DealAmount from Rate × NetWeight
    [NotMapped]
    public decimal CalculatedDealAmount => Rate * (NetWeight ?? 0);

    public TripPhaseStatus Status { get; set; } = TripPhaseStatus.Pending;

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation - Expenses can be linked to a specific phase
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}

public enum TripPhaseType { Up = 1, Down = 2 }
public enum TripPhaseStatus { Pending = 1, InProgress = 2, Completed = 3, Cancelled = 4 }


// ═══════════════════════════════════════════════════
//  TRIP PAYMENT (LEDGER)
// ═══════════════════════════════════════════════════
public class TripPayment : TenantBaseEntity
{
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public PaymentType PaymentType { get; set; }  // Received or Paid

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentMode PaymentMode { get; set; }  // Cash, Bank, UPI, Cheque

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }  // Cheque No, UTR, etc.

    [MaxLength(200)]
    public string? PayerPayee { get; set; }  // Who paid / received

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ReceiptPath { get; set; }  // Attached receipt/voucher
}

public enum PaymentType { Received = 1, Paid = 2 }
public enum PaymentMode { Cash = 1, BankTransfer = 2, UPI = 3, Cheque = 4, Other = 5 }

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

    // Optional: Link expense to a specific phase (UP/DOWN)
    public int? TripPhaseId { get; set; }
    public TripPhase? TripPhase { get; set; }

    // Dynamic category from master table
    public int? CategoryId { get; set; }
    public ExpenseCategoryMaster? CategoryMaster { get; set; }

    // Keep enum for backward compatibility (will be phased out)
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

    // Helper to get category name (from master or enum)
    public string CategoryName => CategoryMaster?.Name ?? Category.ToString();

    // Helper to get phase type name
    [NotMapped]
    public string PhaseName => TripPhase?.PhaseType.ToString() ?? "General";
}

// ═══════════════════════════════════════════════════
//  EXPENSE CATEGORY (Master - Global for all tenants)
// ═══════════════════════════════════════════════════
public class ExpenseCategoryMaster : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Icon { get; set; }  // e.g., "fas fa-gas-pump"

    [MaxLength(20)]
    public string? Color { get; set; } // e.g., "primary", "danger"

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Expense> Expenses { get; set; } = [];
}

// Keep enum for backward compatibility during migration
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
