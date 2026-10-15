namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Application.Persistence;
using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Repositories;
using MatosKC.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

[Collection(InfrastructureTestCollection.Name)]
public sealed class EquipmentAuditPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task AuditAndEquipment_AreAtomicAndRetainActorAndOriginalStatus()
    {
        var actor = new AuditContext { AccountId = Guid.NewGuid(), Reason = "Décision Admin" };
        await using var source = fixture.CreateDbContext();
        var options = new DbContextOptionsBuilder<MatosKCDbContext>()
            .UseNpgsql(source.Database.GetConnectionString()).Options;
        await using var db = new MatosKCDbContext(options, actor);
        var category = new EquipmentCategory("Audit " + Guid.NewGuid(), null);
        var equipment = new Equipment("Original", category.Id, Guid.NewGuid().ToString());
        db.AddRange(category, equipment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using (var transaction = await db.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            equipment.ChangeStatus(EquipmentStatus.Decommissioned);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        db.ChangeTracker.Clear();
        Assert.Equal(EquipmentStatus.Available,
            (await db.Equipments.SingleAsync(item => item.Id == equipment.Id, cancellationToken: TestContext.Current.CancellationToken)).Status);
        Assert.Single(await new AuditRepository(db).ListAsync("Equipment", equipment.Id, 0, 50, TestContext.Current.CancellationToken));

        var saved = await db.Equipments.SingleAsync(item => item.Id == equipment.Id, cancellationToken: TestContext.Current.CancellationToken);
        saved.ChangeStatus(EquipmentStatus.Decommissioned);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var history = await new AuditRepository(db).ListAsync("Equipment", equipment.Id, 0, 50, TestContext.Current.CancellationToken);
        var change = history.Single(entry => entry.Operation == "Modified");
        Assert.Equal(actor.AccountId, change.AccountId);
        Assert.Equal(actor.Reason, change.Reason);
        Assert.Contains("Available", change.ChangesJson);
    }

    [Fact]
    public async Task ThumbnailProjection_IsDeterministicAndDoesNotDuplicateEquipment()
    {
        await using var db = fixture.CreateDbContext();
        var category = new EquipmentCategory("Thumb " + Guid.NewGuid(), null);
        var equipment = new Equipment("Photos", category.Id, Guid.NewGuid().ToString());
        var withoutPhoto = new Equipment("Without", category.Id, Guid.NewGuid().ToString());
        var time = new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);
        var first = new EquipmentPhoto(Guid.NewGuid(), equipment.Id, Guid.NewGuid().ToString(), "first.png", "image/png", 12, time);
        var second = new EquipmentPhoto(Guid.NewGuid(), equipment.Id, Guid.NewGuid().ToString(), "second.png", "image/png", 12, time.AddSeconds(1));
        db.AddRange(category, equipment, withoutPhoto, first, second);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var photos = await new EquipmentThumbnailRepository(db).GetAsync([equipment.Id, withoutPhoto.Id], TestContext.Current.CancellationToken);
        Assert.Equal(first.Id, Assert.Single(photos).Value.PhotoId);
    }

    [Fact]
    public async Task ConcurrentPhotoReplacement_RejectsStaleMetadata()
    {
        Guid photoId = Guid.NewGuid();
        await using var original = fixture.CreateDbContext();
        var category = new EquipmentCategory("Concurrency " + Guid.NewGuid(), null);
        var equipment = new Equipment("Photos", category.Id, Guid.NewGuid().ToString());
        original.AddRange(category, equipment,
            new EquipmentPhoto(photoId, equipment.Id, Guid.NewGuid().ToString(), "old.png", "image/png", 12, DateTimeOffset.UtcNow));
        await original.SaveChangesAsync(TestContext.Current.CancellationToken);
        await using var one = fixture.CreateDbContext();
        await using var two = fixture.CreateDbContext();
        var a = await one.EquipmentPhotos.SingleAsync(photo => photo.Id == photoId, cancellationToken: TestContext.Current.CancellationToken);
        var b = await two.EquipmentPhotos.SingleAsync(photo => photo.Id == photoId, cancellationToken: TestContext.Current.CancellationToken);
        a.ReplaceContent("generation-one-" + photoId, "new.png", "image/png", 20, DateTimeOffset.UtcNow);
        b.ReplaceContent("generation-two-" + photoId, "new.png", "image/png", 20, DateTimeOffset.UtcNow);
        await new EquipmentPhotoRepository(one).UpdateAsync(a, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<MatosKC.Application.Inspections.InspectionOperationException>(() =>
            new EquipmentPhotoRepository(two).UpdateAsync(b, TestContext.Current.CancellationToken));
        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public async Task TemplateSeed_IsIdempotentAndNeverSupersedesExistingTemplate()
    {
        await using var db = fixture.CreateDbContext();
        var categoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        if (!await db.EquipmentCategories.AnyAsync(category => category.Id == categoryId, cancellationToken: TestContext.Current.CancellationToken))
        {
            db.EquipmentCategories.Add(new(categoryId, "Seed " + Guid.NewGuid(), null));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await DevelopmentInspectionTemplates.SeedAsync(db, TestContext.Current.CancellationToken);
        var ids = await db.InspectionTemplates.Where(template => template.EquipmentCategoryId == categoryId)
            .Select(template => template.Id).ToArrayAsync(TestContext.Current.CancellationToken);
        await DevelopmentInspectionTemplates.SeedAsync(db, TestContext.Current.CancellationToken);
        Assert.Equal(ids, await db.InspectionTemplates.Where(template => template.EquipmentCategoryId == categoryId)
            .Select(template => template.Id).ToArrayAsync(TestContext.Current.CancellationToken));
    }
}
