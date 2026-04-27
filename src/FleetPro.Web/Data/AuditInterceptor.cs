using FleetPro.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace FleetPro.Data;

/// <summary>
/// EF Core interceptor that automatically logs Create/Update/Delete operations to AuditLog
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _hca;

    public AuditSaveChangesInterceptor(IHttpContextAccessor hca)
    {
        _hca = hca;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext db)
        {
            await LogChangesAsync(db);
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task LogChangesAsync(AppDbContext db)
    {
        var user = _hca.HttpContext?.User;
        var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var tenantIdStr = user?.FindFirst("TenantId")?.Value;
        var ipAddress = _hca.HttpContext?.Connection.RemoteIpAddress?.ToString();

        int? currentUserId = int.TryParse(userId, out var uid) ? uid : null;
        int? currentTenantId = int.TryParse(tenantIdStr, out var tid) ? tid : null;

        var entries = db.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not AuditLog) // Don't log AuditLog changes
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType().Name;
            var action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => "Unknown"
            };

            // Skip certain entities that generate too much noise
            if (entityType is "MenuItem" or "Permission" or "RolePermission")
                continue;

            string? description = null;
            string? oldValues = null;
            string? newValues = null;

            // Get tenant ID from entity if available
            int? entityTenantId = currentTenantId;
            if (entry.Entity is TenantBaseEntity tenantEntity && tenantEntity.TenantId > 0)
                entityTenantId = tenantEntity.TenantId;

            try
            {
                // Build description based on entity type
                description = BuildDescription(entry, entityType, action);

                if (entry.State == EntityState.Modified)
                {
                    var changes = new Dictionary<string, object?>();
                    var original = new Dictionary<string, object?>();

                    foreach (var prop in entry.Properties.Where(p => p.IsModified))
                    {
                        // Skip audit fields and navigation properties
                        if (prop.Metadata.Name is "UpdatedAt" or "UpdatedBy")
                            continue;

                        original[prop.Metadata.Name] = prop.OriginalValue;
                        changes[prop.Metadata.Name] = prop.CurrentValue;
                    }

                    if (changes.Any())
                    {
                        oldValues = JsonSerializer.Serialize(original);
                        newValues = JsonSerializer.Serialize(changes);
                    }
                }
                else if (entry.State == EntityState.Added)
                {
                    var values = new Dictionary<string, object?>();
                    foreach (var prop in entry.Properties)
                    {
                        // Skip audit fields and sensitive data
                        if (prop.Metadata.Name is "PasswordHash" or "RefreshToken")
                            continue;
                        if (prop.CurrentValue != null)
                            values[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    if (values.Any())
                        newValues = JsonSerializer.Serialize(values);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    var values = new Dictionary<string, object?>();
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.Metadata.Name is "PasswordHash" or "RefreshToken")
                            continue;
                        if (prop.OriginalValue != null)
                            values[prop.Metadata.Name] = prop.OriginalValue;
                    }
                    if (values.Any())
                        oldValues = JsonSerializer.Serialize(values);
                }
            }
            catch
            {
                // If serialization fails, just log without values
            }

            // Skip if no meaningful changes for updates
            if (entry.State == EntityState.Modified && string.IsNullOrEmpty(newValues))
                continue;

            var auditLog = new AuditLog
            {
                TenantId = entityTenantId,
                UserId = currentUserId,
                Module = entityType,
                Action = action,
                Description = description,
                IpAddress = ipAddress,
                OldValues = oldValues,
                NewValues = newValues,
                CreatedAt = DateTime.UtcNow
            };

            db.AuditLogs.Add(auditLog);
        }
    }

    private static string BuildDescription(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string entityType, string action)
    {
        return entityType switch
        {
            "Truck" => $"{action} truck: {GetPropertyValue(entry, "NumberPlate")}",
            "Driver" => $"{action} driver: {GetPropertyValue(entry, "FullName")}",
            "Trip" => $"{action} trip: {GetPropertyValue(entry, "TripNumber")}",
            "Expense" => $"{action} expense: ₹{GetPropertyValue(entry, "Amount")}",
            "ApplicationUser" => $"{action} user: {GetPropertyValue(entry, "Email")}",
            "Tenant" => $"{action} company: {GetPropertyValue(entry, "CompanyName")}",
            "UserRole" => $"{action} user role assignment",
            "UserPermission" => $"{action} user permission",
            "ExpenseCategoryMaster" => $"{action} expense category: {GetPropertyValue(entry, "Name")}",
            "Alert" => $"{action} alert: {GetPropertyValue(entry, "Title")}",
            "TripDocument" => $"{action} document: {GetPropertyValue(entry, "FileName")}",
            "Role" => $"{action} role: {GetPropertyValue(entry, "Name")}",
            _ => $"{action} {entityType}"
        };
    }

    private static string? GetPropertyValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        return prop?.CurrentValue?.ToString() ?? prop?.OriginalValue?.ToString();
    }
}
