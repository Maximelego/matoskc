namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Application.Persistence;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

public sealed class EquipmentAuditUnitTests
{
    private static MatosKCDbContext CreateDb() => new(new DbContextOptionsBuilder<MatosKCDbContext>()
        .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);

    [Fact]
    public void Audit_CapturesOriginalValuesActorReasonAndTransition()
    {
        using var db = CreateDb();
        var equipment = new Equipment("Before", Guid.NewGuid(), "SERIAL");
        db.Attach(equipment);
        equipment.ChangeStatus(EquipmentStatus.Decommissioned);
        equipment.UpdateDetails("After", equipment.CategoryId);
        var actor = new AuditContext { AccountId = Guid.NewGuid(), Reason = "Hors service" };

        var record = Assert.Single(AuditCollector.Collect(db, actor));

        Assert.Equal(actor.AccountId, record.AccountId);
        Assert.Equal(actor.Reason, record.Reason);
        Assert.Contains("Available", record.ChangesJson);
        Assert.Contains("Decommissioned", record.ChangesJson);
        Assert.Contains("Before", record.ChangesJson);
        Assert.Contains("After", record.ChangesJson);
    }

    [Fact]
    public void Audit_DoesNotCopyCredentialContents_OrJournalItself()
    {
        using var db = CreateDb();
        db.Accounts.Add(new Account("Private Name", "private@example.test", "SECRET-HASH", true, Role.SuperAdmin, null));
        db.AuditEntries.Add(new(Guid.NewGuid(), "Equipment", Guid.NewGuid(), "Added", null,
            DateTimeOffset.UtcNow, null, "[]"));

        var record = Assert.Single(AuditCollector.Collect(db, null));
        Assert.DoesNotContain("SECRET-HASH", record.ChangesJson);
        Assert.DoesNotContain("private@example.test", record.ChangesJson);
        Assert.DoesNotContain("Private Name", record.ChangesJson);
        Assert.Null(record.AccountId);
    }

    [Fact]
    public void Seed_ProvidesFiveTemplatesWithMultilineDetailsAndStableIdentifiers()
    {
        var templates = DevelopmentInspectionTemplates.Create();
        Assert.Equal(5, templates.Count);
        Assert.Equal(5, templates.Select(template => template.EquipmentCategoryId).Distinct().Count());
        Assert.All(templates, template => Assert.All(template.Points, point =>
        {
            Assert.Contains("\n\t•", point.DescriptionDetail);
            Assert.Equal(template.Id, point.TemplateId);
            Assert.Null(point.PhotoId);
        }));
        Assert.Contains("Boîtier d’alimentation", templates[0].Points[0].DescriptionDetail);
        Assert.Equal(templates.Select(template => template.Id),
            DevelopmentInspectionTemplates.Create().Select(template => template.Id));
    }
}
