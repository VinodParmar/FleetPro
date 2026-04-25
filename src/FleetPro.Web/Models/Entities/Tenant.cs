using System.ComponentModel.DataAnnotations;

namespace FleetPro.Models.Entities;

public class Tenant : BaseEntity
{
    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Subdomain { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ContactPerson { get; set; }

    [MaxLength(150)]
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

    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }

    // Navigation
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Truck> Trucks { get; set; } = new List<Truck>();
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

public enum TenantPlan { Starter = 1, Business = 2, Premium = 3 }
public enum TenantStatus { Active = 1, Trial = 2, Inactive = 3, Suspended = 4 }
