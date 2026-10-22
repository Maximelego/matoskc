namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Application.Agencies;
using MatosKC.Application.Files.Ports;
using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Hash;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class AgencyIsolationTests
{
    private readonly InMemoryDatabaseRoot root = new();
    private readonly string name = Guid.NewGuid().ToString();
    private MatosKCDbContext Db(AgencyScope? scope = null) => new(
        new DbContextOptionsBuilder<MatosKCDbContext>().UseInMemoryDatabase(name, root).Options, agencyScope: scope);
    private static InspectionActor Actor(Role role, Guid? agency) => new(Guid.NewGuid(), role, null, agency);

    private async Task Seed()
    {
        await using var db = Db();
        await new DevelopmentDataSeeder(db, new Storage(), new IdentityPasswordHasher()).SeedAsync();
    }

    [Theory]
    [InlineData(Role.Admin, 83)]
    [InlineData(Role.Agency, 33)]
    [InlineData(Role.Admin, 12)]
    public async Task EveryOwnedResourceAndChildIsFilteredForTheAuthenticatedAgency(Role role, int code)
    {
        await Seed();
        Guid owner; Guid foreignEquipment; Guid foreignTemplate; Guid foreignCategory;
        await using (var system = Db())
        {
            owner = (await system.Agencies.SingleAsync(value => value.Code == code)).Id;
            foreignEquipment = await system.Equipments.Where(value => value.AgencyId != owner).Select(value => value.Id).FirstAsync();
            foreignTemplate = await system.InspectionTemplates.Where(value => value.AgencyId != owner).Select(value => value.Id).FirstAsync();
            foreignCategory = await system.EquipmentCategories.Where(value => value.AgencyId != owner).Select(value => value.Id).FirstAsync();
        }
        var scope = new AgencyScope(); scope.SetActor(Actor(role, owner));
        await using var db = Db(scope);
        Assert.Equal(15, await db.Equipments.CountAsync());
        Assert.Equal(5, await db.EquipmentCategories.CountAsync());
        Assert.Equal(5, await db.InspectionTemplates.CountAsync());
        Assert.Equal(7, await db.Rentals.CountAsync());
        Assert.Equal(10, await db.Inspections.CountAsync());
        Assert.Equal(3, await db.EquipmentDefects.CountAsync());
        Assert.Equal(3, await db.DefectOccurrences.CountAsync());
        Assert.Equal(1, await db.EquipmentMaintenances.CountAsync());
        Assert.Equal(5, await db.EquipmentPhotos.CountAsync());
        Assert.Equal(3, await db.InspectionMedia.CountAsync());
        Assert.All(await db.Equipments.ToArrayAsync(), value => Assert.Equal(owner, value.AgencyId));
        Assert.Null(await db.Equipments.SingleOrDefaultAsync(value => value.Id == foreignEquipment));
        Assert.Null(await db.InspectionTemplates.SingleOrDefaultAsync(value => value.Id == foreignTemplate));
        Assert.Null(await db.EquipmentCategories.SingleOrDefaultAsync(value => value.Id == foreignCategory));
        Assert.Equal(1, await db.Equipments.Skip(14).Take(10).CountAsync());
    }

    [Fact]
    public void PostgreSqlCanTranslateAgencyFiltersForEveryPrivateResource()
    {
        var scope = new AgencyScope(); scope.SetActor(Actor(Role.Admin, Guid.NewGuid()));
        using var db = new MatosKCDbContext(new DbContextOptionsBuilder<MatosKCDbContext>()
            .UseNpgsql("Host=unused;Database=unused;Username=unused;Password=unused").Options, agencyScope: scope);
        foreach (var query in new[] { db.Equipments.ToQueryString(), db.EquipmentCategories.ToQueryString(),
            db.Rentals.ToQueryString(), db.Inspections.ToQueryString(), db.InspectionTemplates.ToQueryString(),
            db.InspectionTemplatePhotos.ToQueryString(), db.InspectionMedia.ToQueryString(),
            db.EquipmentPhotos.ToQueryString(), db.EquipmentDefects.ToQueryString(),
            db.DefectOccurrences.ToQueryString(), db.EquipmentMaintenances.ToQueryString(),
            db.Set<InspectionAnswer>().ToQueryString(), db.Set<InspectionDefectReview>().ToQueryString(),
            db.Set<InspectionFinding>().ToQueryString(), db.Set<InspectionTemplatePoint>().ToQueryString() })
            Assert.Contains("agency_id", query);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task SuperAdminHasGlobalOrExplicitlySelectedAgencyAccess_AndFiltersUseEachContextsScope()
    {
        await Seed();
        var global = new AgencyScope(); global.SetActor(Actor(Role.SuperAdmin, null));
        await using var db = Db(global);
        Assert.Equal(45, await db.Equipments.CountAsync());
        var owner = (await db.Agencies.SingleAsync(value => value.Code == 33)).Id;
        var selected = new AgencyScope(); selected.SetActor(Actor(Role.SuperAdmin, null), owner);
        await using var scoped = Db(selected);
        Assert.Equal(15, await scoped.Equipments.CountAsync());
        Assert.All(await scoped.InspectionTemplates.ToArrayAsync(), value => Assert.Equal(owner, value.AgencyId));
        Assert.Equal(45, await db.Equipments.CountAsync());
    }

    [Fact]
    public async Task AnonymousAndForeignWritesCannotUseSystemFallback()
    {
        await Seed();
        var anonymous = new AgencyScope(); anonymous.SetActor(null);
        await using (var db = Db(anonymous)) Assert.Empty(await db.Equipments.ToArrayAsync());
        Guid own; Guid other;
        await using (var db = Db())
        {
            own = (await db.Agencies.SingleAsync(value => value.Code == 83)).Id;
            other = (await db.Agencies.SingleAsync(value => value.Code == 33)).Id;
        }
        var scope = new AgencyScope(); scope.SetActor(Actor(Role.Admin, own));
        var error = Assert.Throws<InspectionOperationException>(() => scope.ResolveOwner(other));
        Assert.Equal(403, error.StatusCode);
        Assert.Equal(own, scope.ResolveOwner(null));
        await using var scoped = Db(scope);
        var category = new EquipmentCategory("Injection", null); category.AssignAgency(other); scoped.Add(category);
        error = await Assert.ThrowsAsync<InspectionOperationException>(() => scoped.SaveChangesAsync());
        Assert.Equal(403, error.StatusCode);
        var missing = new AgencyScope();
        Assert.Throws<InspectionOperationException>(() => missing.SetActor(Actor(Role.Admin, null)));
    }

    [Fact]
    public async Task SeederIsIdempotentKeepsAgencyOwnershipAndDoesNotReplaceOperatorsTemplates()
    {
        var storage = new Storage();
        await using var db = Db();
        var seeder = new DevelopmentDataSeeder(db, storage, new IdentityPasswordHasher());
        await seeder.SeedAsync();
        var owner = (await db.Agencies.SingleAsync(value => value.Code == 83)).Id;
        var original = await db.InspectionTemplates.FirstAsync(value => value.AgencyId == owner);
        var id = Guid.NewGuid();
        var custom = new InspectionTemplate(id,
            [new InspectionTemplatePoint(Guid.NewGuid(), id, "Contrôle personnalisé", "Détail", null)], original.EquipmentCategoryId);
        custom.AssignAgency(owner); db.Add(custom); await db.SaveChangesAsync();
        await seeder.SeedAsync();
        Assert.Equal(45, await db.Equipments.CountAsync());
        Assert.Equal(15, await db.EquipmentCategories.CountAsync());
        Assert.Equal(16, await db.InspectionTemplates.CountAsync());
        Assert.Equal(21, await db.Rentals.CountAsync());
        Assert.Equal(30, await db.Inspections.CountAsync());
        Assert.Equal(3, await db.EquipmentMaintenances.CountAsync());
        Assert.Equal(24, storage.Count);
        Assert.Equal(3, await db.EquipmentDefects.CountAsync(value => value.Status == MatosKC.Domain.Entities.Maintenance.EquipmentDefectStatus.Resolved));
        Assert.Equal(6, await db.EquipmentDefects.CountAsync(value => value.Status == MatosKC.Domain.Entities.Maintenance.EquipmentDefectStatus.Open));
    }

    [Fact]
    public void OwnershipCannotBeTransferredEvenWhenThereIsNoHistory()
    {
        var agency = Guid.NewGuid();
        foreach (var value in new IAgencyOwned[] { new EquipmentCategory("A", null), new Equipment("B", Guid.NewGuid(), "S") })
        {
            Assert.Throws<ArgumentException>(() => value.AssignAgency(Guid.Empty));
            value.AssignAgency(agency); value.AssignAgency(agency);
            Assert.Throws<InvalidOperationException>(() => value.AssignAgency(Guid.NewGuid()));
        }
    }

    private sealed class Storage : IObjectStorage
    {
        private readonly Dictionary<string, byte[]> values = [];
        public int Count => values.Count;
        public Task EnsureReadyAsync(CancellationToken ct = default) => Task.CompletedTask;
        public async Task StoreAsync(string key, Stream content, string type, CancellationToken ct = default)
        { using var buffer = new MemoryStream(); await content.CopyToAsync(buffer, ct); values[key] = buffer.ToArray(); }
        public Task<StoredObject?> RetrieveAsync(string key, CancellationToken ct = default) => Task.FromResult(
            values.TryGetValue(key, out var bytes) ? new StoredObject(new MemoryStream(bytes), "image/png", bytes.Length) : null);
        public Task DeleteAsync(string key, CancellationToken ct = default) { values.Remove(key); return Task.CompletedTask; }
    }
}
