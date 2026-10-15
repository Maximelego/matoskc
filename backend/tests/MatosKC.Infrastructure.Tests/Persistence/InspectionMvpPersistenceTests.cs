namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Application.Inspections;
using MatosKC.Infrastructure.Persistence.Repositories;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

[Collection(InfrastructureTestCollection.Name)]
public sealed class InspectionMvpPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Search_FiltersBeforeCountAndPaginationAndReturnsClientEquipmentContext()
    {
        await using var db = fixture.CreateDbContext();
        var category = new EquipmentCategory("MVP " + Guid.NewGuid(), null);
        var item = new Equipment("Camion", category.Id, Guid.NewGuid().ToString());
        var agency = new Agency("MVP", Random.Shared.Next(100000, int.MaxValue));
        var account = new Account("Inspector", $"mvp-{Guid.NewGuid():N}@example.test", "hash", true, Role.SuperAdmin, null);
        Guid templateId = Guid.NewGuid();
        var template = new InspectionTemplate(templateId,
            [new InspectionTemplatePoint(Guid.NewGuid(), templateId, "Point", "Le point sera à préciser", null)], category.Id);
        var start = new DateTimeOffset(2026, 10, 14, 10, 0, 0, TimeSpan.Zero);
        db.AddRange(category, item, agency, account, template);

        for (int index = 0; index < 3; index++)
        {
            var client = new Client(Guid.NewGuid(), "Client " + index, "client@example.test", "0612345678",
                new CommandNumber(agency.Code, "000123"));
            var rental = new Rental(Guid.NewGuid(), client.Id, item.Id, start, agency.Id);
            var inspection = new InspectionEntity(Guid.NewGuid(), rental.Id, template.Id,
                InspectionType.Departure, account.Id, "Maxime", start.AddDays(index));
            rental.Cancel(start);
            inspection.Cancel(account.Id, "Abandon", start.AddDays(index));
            db.AddRange(client, rental, inspection);
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new InspectionSearchRepository(db);
        var filter = new InspectionSearchQuery(EquipmentId: item.Id, AgencyId: agency.Id,
            CommandNumber: "000123", From: start, To: start.AddDays(2), Status: InspectionStatus.Cancelled, Offset: 1, Limit: 1);
        var page = await repository.SearchAsync(filter, TestContext.Current.CancellationToken);
        Assert.Equal(3, page.Total);
        var result = Assert.Single(page.Inspections);
        Assert.Equal(start.AddDays(1), result.StartedAt);
        Assert.Equal("Camion", result.EquipmentName);
        Assert.Equal("Client 1", result.ClientName);
        Assert.Equal("000123", result.CommandNumber);
        Assert.Equal(agency.Id, result.AgencyId);
        var beyond = await repository.SearchAsync(filter with { Offset = 100 }, TestContext.Current.CancellationToken);
        Assert.Equal(3, beyond.Total);
        Assert.Empty(beyond.Inspections);
        var foreign = await repository.SearchAsync(filter with { AgencyId = Guid.NewGuid() }, TestContext.Current.CancellationToken);
        Assert.Equal(0, foreign.Total);
    }

    [Fact]
    public async Task CancelledReturn_DoesNotBlockReplacementButTwoLiveReturnsRemainForbidden()
    {
        await using var db = fixture.CreateDbContext();
        var category = new EquipmentCategory("MVP " + Guid.NewGuid(), null);
        var item = new Equipment("Camion", category.Id, Guid.NewGuid().ToString());
        var account = new Account("Inspector", $"mvp-{Guid.NewGuid():N}@example.test", "hash", true, Role.SuperAdmin, null);
        var client = new Client(Guid.NewGuid(), "Client", "client@example.test", "0612345678", new CommandNumber(83, "000123"));
        var now = DateTimeOffset.UtcNow;
        var rental = new Rental(Guid.NewGuid(), client.Id, item.Id, now);
        Guid templateId = Guid.NewGuid();
        var template = new InspectionTemplate(templateId,
            [new InspectionTemplatePoint(Guid.NewGuid(), templateId, "Point", "Le point sera à préciser", null)], category.Id);
        var old = new InspectionEntity(Guid.NewGuid(), rental.Id, template.Id, InspectionType.Return, account.Id, "Maxime", now);
        old.Cancel(account.Id, "Recommencer", now);
        var next = new InspectionEntity(Guid.NewGuid(), rental.Id, template.Id, InspectionType.Return, account.Id, "Maxime", now);
        db.AddRange(category, item, account, client, rental, template, old, next);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        var resumed = await new InspectionsRepository(db).GetForRentalAsync(rental.Id, InspectionType.Return, TestContext.Current.CancellationToken);
        Assert.Equal(next.Id, resumed!.Id);
        var cancelled = await db.Inspections.SingleAsync(value => value.Id == old.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Recommencer", cancelled.CancellationReason);
        Assert.Equal(account.Id, cancelled.CancelledByAccountId);
        db.Inspections.Add(new InspectionEntity(Guid.NewGuid(), rental.Id, template.Id, InspectionType.Return, account.Id, "Maxime", now));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
