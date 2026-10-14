namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Repositories;
using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

[Collection(InfrastructureTestCollection.Name)]
public sealed class InspectionPersistenceTests(PostgreSqlFixture fixture)
{
    private static (Client Client, Rental Rental, InspectionTemplate Template, InspectionEntity Inspection,
        Equipment Equipment, EquipmentCategory Category, Account Account) Data()
    {
        var category = new EquipmentCategory("Category " + Guid.NewGuid(), null);
        var equipment = new Equipment("Equipment", category.Id, Guid.NewGuid().ToString());
        var client = new Client(Guid.NewGuid(), "Client", "client@example.test", "0612345678", new CommandNumber(83, "001234"));
        var rental = new Rental(Guid.NewGuid(), client.Id, equipment.Id, DateTimeOffset.UtcNow);
        Guid templateId = Guid.NewGuid();
        var template = new InspectionTemplate(templateId,
            [new InspectionTemplatePoint(Guid.NewGuid(), templateId, "Point", null)], category.Id);
        var account = new Account("Inspector", $"inspect-{Guid.NewGuid():N}@example.test", "hash", true, Role.SuperAdmin, null);
        var inspection = new InspectionEntity(Guid.NewGuid(), rental.Id, template.Id,
            InspectionType.Departure, account.Id, "Maxime", DateTimeOffset.UtcNow);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.NonCompliant, "Défaut");
        inspection.AddAnswerPhoto(inspection.Answers[0].Id, Guid.NewGuid());
        inspection.Sign("Client", [1, 2], DateTimeOffset.UtcNow);

        return (client, rental, template, inspection, equipment, category, account);
    }

    [Fact]
    public async Task RoundTrip_LoadsClientCommandTemplatePointsAnswersAndPhotos()
    {
        var data = Data();
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(data.Category, data.Equipment, data.Account);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var work = new EfUnitOfWork(db);
            await work.ExecuteAsync(async ct =>
            {
                await new ClientRepository(db).AddClientAsync(data.Client, ct);
                await new RentalRepository(db).AddAsync(data.Rental, ct);
                await new InspectionTemplateRepository(db).AddAsync(data.Template, ct);
                await new InspectionsRepository(db).AddInspectionAsync(data.Inspection, ct);
                return true;
            }, TestContext.Current.CancellationToken);
        }

        await using (var db = fixture.CreateDbContext())
        {
            var client = await new ClientRepository(db).GetByIdAsync(data.Client.Id, TestContext.Current.CancellationToken);
            Assert.Equal(new CommandNumber(83, "001234"), client!.CommandNumber);
            var template = await new InspectionTemplateRepository(db).GetLatestForCategoryAsync(data.Category.Id, TestContext.Current.CancellationToken);
            Assert.Equal(data.Template.Id, template!.Id);
            Assert.Single(template.Points);
            var inspection = await new InspectionsRepository(db).GetInspectionByIdAsync(data.Inspection.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Client", inspection!.SignerName);
            Assert.Equal(new byte[] { 1, 2 }, inspection.SignaturePng);
            Assert.NotNull(inspection.SignedAt);
            Assert.NotNull(inspection.AcceptanceText);
            var answer = Assert.Single(inspection.Answers);
            Assert.Equal("Défaut", answer.Comment);
            Assert.Equal(data.Inspection.Answers[0].PhotosIds[0], Assert.Single(answer.PhotosIds));
            inspection.SetAnswer(answer.TemplatePointId, InspectionResult.NonCompliant, "Modifié");
            inspection.AddAnswerPhoto(answer.Id, Guid.NewGuid());
            await new EfUnitOfWork(db).ExecuteAsync(async ct =>
            {
                await new InspectionsRepository(db).UpdateInspectionAsync(inspection, ct);
                return true;
            }, TestContext.Current.CancellationToken);
        }

        await using (var db = fixture.CreateDbContext())
        {
            var inspection = await new InspectionsRepository(db).GetInspectionByIdAsync(data.Inspection.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Modifié", inspection!.Answers[0].Comment);
            Assert.Equal(2, inspection.Answers[0].PhotosIds.Count);
        }
    }

    [Fact]
    public async Task UnitOfWork_RollsBackWritesAndClearsTrackedEntitiesOnFailure()
    {
        var data = Data();
        await using var db = fixture.CreateDbContext();
        var work = new EfUnitOfWork(db);
        await Assert.ThrowsAsync<MatosKC.Application.Inspections.InspectionOperationException>(() => work.ExecuteAsync<bool>(async ct =>
        {
            db.Clients.Add(data.Client);
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Failure after SQL write.");
        }, TestContext.Current.CancellationToken));
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.False(await db.Clients.AnyAsync(client => client.Id == data.Client.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Database_RejectsDuplicateOpenRentalsForEquipment()
    {
        var data = Data();
        await using var db = fixture.CreateDbContext();
        db.AddRange(data.Category, data.Equipment, data.Client, data.Rental);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Rentals.Add(new Rental(Guid.NewGuid(), data.Client.Id, data.Equipment.Id, DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConcurrentEquipmentUpdates_UseOptimisticConcurrency()
    {
        var data = Data();
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(data.Category, data.Equipment);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var first = fixture.CreateDbContext();
        await using var second = fixture.CreateDbContext();
        var a = await first.Equipments.SingleAsync(value => value.Id == data.Equipment.Id, TestContext.Current.CancellationToken);
        var b = await second.Equipments.SingleAsync(value => value.Id == data.Equipment.Id, TestContext.Current.CancellationToken);
        a.ChangeStatus(EquipmentStatus.Borrowed);
        b.ChangeStatus(EquipmentStatus.Decommissioned);
        await first.SaveChangesAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
