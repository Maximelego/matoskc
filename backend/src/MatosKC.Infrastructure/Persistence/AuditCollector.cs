namespace MatosKC.Infrastructure.Persistence;

using System.Text.Json;
using MatosKC.Application.Persistence;
using MatosKC.Domain.Entities;
using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;

public static class AuditCollector
{
    public static IReadOnlyList<AuditEntry> Collect(MatosKCDbContext db, AuditContext? actor)
    {
        db.ChangeTracker.DetectChanges();
        var records = new List<AuditEntry>();

        foreach (var entry in db.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            // Never journal credentials, personal client fields, signature or report bytes.
            // Other entities retain operation + changed field names, without field contents.
            bool equipmentDetails = entry.Entity is Equipment or EquipmentCategory;
            string[] safeFields = ["Status", "Type", "RentalId", "TemplateId", "EquipmentId",
                "EquipmentCategoryId", "AgencyId", "StartedAt", "ValidatedAt", "CancelledAt",
                "CreatedByAccountId", "CancelledByAccountId", "TemplatePointId", "Position", "Result"];

            var changes = entry.Properties
                .Where(property => !property.Metadata.IsPrimaryKey()
                    && !property.Metadata.IsConcurrencyToken
                    && (entry.State != EntityState.Modified || property.IsModified))
                .Select(property => new
                {
                    field = property.Metadata.Name,
                    before = (equipmentDetails || safeFields.Contains(property.Metadata.Name)) && entry.State != EntityState.Added
                        ? property.OriginalValue?.ToString() : null,
                    after = (equipmentDetails || safeFields.Contains(property.Metadata.Name)) && entry.State != EntityState.Deleted
                        ? property.CurrentValue?.ToString() : null
                }).ToArray();

            records.Add(new AuditEntry(Guid.NewGuid(), entry.Metadata.ClrType.Name,
                entry.Entity.Id, entry.State.ToString(), actor?.AccountId,
                DateTimeOffset.UtcNow, actor?.Reason, JsonSerializer.Serialize(changes)));
        }

        return records;
    }
}
