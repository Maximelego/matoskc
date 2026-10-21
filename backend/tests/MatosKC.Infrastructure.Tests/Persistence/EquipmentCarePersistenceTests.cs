namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Application.Inspections;
using MatosKC.Application.Maintenance;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Maintenance;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

[Collection(InfrastructureTestCollection.Name)]
public sealed class EquipmentCarePersistenceTests(PostgreSqlFixture fixture)
{
    private async Task<(Guid EquipmentId, Guid DefectId, Guid AccountId, Guid AnswerId)> SeedAsync()
    {
        await using var db = fixture.CreateDbContext();
        var category = new EquipmentCategory("Care " + Guid.NewGuid(), null);
        var equipment = new Equipment("Souffleuse", category.Id, Guid.NewGuid().ToString());
        var account = new Account("Atelier", $"care-{Guid.NewGuid():N}@example.test", "hash", true, Role.SuperAdmin, null);
        var client = new Client(Guid.NewGuid(), "Client", "client@example.test", "0612345678", new CommandNumber(83, "001234"));
        var now = DateTimeOffset.UtcNow.AddMinutes(-10);
        var rental = new Rental(Guid.NewGuid(), client.Id, equipment.Id, now);
        Guid templateId = Guid.NewGuid();
        var template = new InspectionTemplate(templateId,
            [new InspectionTemplatePoint(Guid.NewGuid(), templateId, "Tuyau", "Raccords\n\t• Étanchéité", null)], category.Id);
        var inspection = new InspectionEntity(Guid.NewGuid(), rental.Id, template.Id, InspectionType.Departure, account.Id, "Maxime", now);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.NonCompliant, "Déchiré");
        Guid answerId = inspection.Answers[0].Id;
        Guid photoId = Guid.NewGuid();
        inspection.AddAnswerPhoto(answerId, photoId);
        inspection.Sign("Client", [1], now);
        inspection.Validate(template, now);
        inspection.AttachReport([37, 80, 68, 70]);
        var defect = new EquipmentDefect(Guid.NewGuid(), equipment.Id, inspection.Id, answerId, "Tuyau", "Déchiré", now);
        var occurrence = new DefectOccurrence(Guid.NewGuid(), defect.Id, inspection.Id, answerId,
            "Tuyau", "Déchiré", now, [photoId]);
        db.AddRange(category, equipment, account, client, rental, template, inspection, defect, occurrence);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (equipment.Id, defect.Id, account.Id, answerId);
    }

    [Fact]
    public async Task IndependentReviewAndFinding_RoundTripAndRemovedEvidenceStaysDetached()
    {
        var seed = await SeedAsync();
        Guid inspectionId; Guid findingId = Guid.NewGuid(); Guid photoId = Guid.NewGuid();
        await using (var db = fixture.CreateDbContext())
        {
            var defect = await db.EquipmentDefects.SingleAsync(value => value.Id == seed.DefectId, TestContext.Current.CancellationToken);
            var original = await db.Inspections.SingleAsync(value => value.Id == defect.InspectionId, TestContext.Current.CancellationToken);
            var template = await db.InspectionTemplates.Include(value => value.Points)
                .SingleAsync(value => value.Id == original.TemplateId, TestContext.Current.CancellationToken);
            var inspection = new InspectionEntity(Guid.NewGuid(), original.RentalId, template.Id, InspectionType.Return,
                seed.AccountId, "Maxime", DateTimeOffset.UtcNow);
            inspectionId = inspection.Id;
            inspection.PresentDefect(defect.Id, defect.Description, defect.Observation, defect.ReportedAt, [Guid.NewGuid()]);
            inspection.ReviewDefect(defect.Id, DefectReviewResult.Unchanged, null);
            inspection.SetAnswer(template.Points[0].Id, InspectionResult.NonCompliant, null);
            inspection.SetFinding(findingId, template.Points[0].Id, "Nouveau défaut");
            inspection.AddEvidencePhoto(findingId, photoId);
            db.Add(inspection);
            db.Add(InspectionMedia.ForEvidence(photoId, inspection.Id, findingId, false,
                "tests/" + photoId, "photo.png", "image/png", 1));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using (var db = fixture.CreateDbContext())
        {
            var inspection = (await new InspectionsRepository(db).GetInspectionByIdAsync(inspectionId, TestContext.Current.CancellationToken))!;
            Assert.Equal(DefectReviewResult.Unchanged, Assert.Single(inspection.KnownDefects).Result);
            Assert.Equal(photoId, Assert.Single(Assert.Single(inspection.Findings).PhotoIds));
            inspection.RemoveFinding(findingId);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using var read = fixture.CreateDbContext();
        var result = (await new InspectionsRepository(read).GetInspectionByIdAsync(inspectionId, TestContext.Current.CancellationToken))!;
        Assert.Empty(result.Findings);
        Assert.True((await read.Set<InspectionFinding>().SingleAsync(value => value.Id == findingId,
            TestContext.Current.CancellationToken)).IsRemoved);
        Assert.True(await read.Set<InspectionMedia>().AnyAsync(value => value.Id == photoId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MaintenanceAndResolution_RoundTripWithOriginalConstatAndAudit()
    {
        var seed = await SeedAsync();
        Guid maintenanceId;
        await using (var db = fixture.CreateDbContext())
        {
            var repository = new EquipmentCareRepository(db);
            var defect = (await repository.GetDefectAsync(seed.DefectId, TestContext.Current.CancellationToken))!;
            var maintenance = EquipmentMaintenance.Record(Guid.NewGuid(), seed.EquipmentId,
                DateTimeOffset.UtcNow.AddMinutes(-1), "Prestataire", "Tuyau remplacé", seed.AccountId,
                DateTimeOffset.UtcNow, [defect]);
            maintenanceId = maintenance.Id;
            await new EfUnitOfWork(db).ExecuteAsync(async ct =>
            {
                await repository.AddMaintenanceAsync(maintenance, ct);
                return true;
            }, TestContext.Current.CancellationToken);
        }

        await using var read = fixture.CreateDbContext();
        var care = new EquipmentCareRepository(read);
        var resolved = await care.GetDefectAsync(seed.DefectId, TestContext.Current.CancellationToken);
        Assert.Equal(EquipmentDefectStatus.Resolved, resolved!.Status);
        Assert.Equal(maintenanceId, resolved.ResolvedByMaintenanceId);
        Assert.Equal(seed.AccountId, resolved.ResolvedByAccountId);
        var maintenanceSaved = await care.GetMaintenanceAsync(maintenanceId, TestContext.Current.CancellationToken);
        Assert.Equal(seed.DefectId, Assert.Single(maintenanceSaved!.ResolvedDefectIds));
        var source = await read.Set<InspectionAnswer>().SingleAsync(answer => answer.Id == seed.AnswerId,
            TestContext.Current.CancellationToken);
        Assert.Equal("Déchiré", source.Comment);
        Assert.Single(source.PhotosIds);
        Assert.Single(await care.GetOccurrencesAsync([seed.DefectId], TestContext.Current.CancellationToken));
        Assert.Equal(0, (await care.ListDefectsAsync(seed.EquipmentId, EquipmentDefectStatus.Open, 0, 50,
            TestContext.Current.CancellationToken)).Total);
        Assert.True(await read.AuditEntries.AnyAsync(entry => entry.EntityId == maintenanceId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FailedCommit_RollsBackMaintenanceAndResolutionTogether()
    {
        var seed = await SeedAsync();
        Guid maintenanceId = Guid.NewGuid();
        await using var db = fixture.CreateDbContext();
        var care = new EquipmentCareRepository(db);
        await Assert.ThrowsAsync<InspectionOperationException>(() => new EfUnitOfWork(db).ExecuteAsync<bool>(async ct =>
        {
            var defect = (await care.GetDefectAsync(seed.DefectId, ct))!;
            var maintenance = EquipmentMaintenance.Record(maintenanceId, seed.EquipmentId,
                DateTimeOffset.UtcNow.AddMinutes(-1), "Atelier", "Réparation", seed.AccountId,
                DateTimeOffset.UtcNow, [defect]);
            await care.AddMaintenanceAsync(maintenance, ct);
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Failure after SQL write.");
        }, TestContext.Current.CancellationToken));

        Assert.Null(await care.GetMaintenanceAsync(maintenanceId, TestContext.Current.CancellationToken));
        Assert.Equal(EquipmentDefectStatus.Open,
            (await care.GetDefectAsync(seed.DefectId, TestContext.Current.CancellationToken))!.Status);
        Assert.False(await db.AuditEntries.AnyAsync(entry => entry.EntityId == maintenanceId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Database_RejectsDuplicateOccurrenceForSameInspectionAnswer()
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateDbContext();
        var original = await db.DefectOccurrences.SingleAsync(occurrence => occurrence.DefectId == seed.DefectId,
            TestContext.Current.CancellationToken);
        db.DefectOccurrences.Add(new DefectOccurrence(Guid.NewGuid(), original.DefectId, original.InspectionId,
            original.InspectionAnswerId!.Value, original.Description, original.Observation, original.ObservedAt, original.PhotoIds));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConcurrentMaintenance_CannotResolveSameDefectTwice()
    {
        var seed = await SeedAsync();
        await using var one = fixture.CreateDbContext();
        await using var two = fixture.CreateDbContext();
        var first = (await one.EquipmentDefects.SingleAsync(defect => defect.Id == seed.DefectId,
            TestContext.Current.CancellationToken));
        var second = (await two.EquipmentDefects.SingleAsync(defect => defect.Id == seed.DefectId,
            TestContext.Current.CancellationToken));
        var now = DateTimeOffset.UtcNow;
        var a = EquipmentMaintenance.Record(Guid.NewGuid(), seed.EquipmentId, now, "A", "Réparation", seed.AccountId, now, [first]);
        var b = EquipmentMaintenance.Record(Guid.NewGuid(), seed.EquipmentId, now, "B", "Réparation", seed.AccountId, now, [second]);
        one.EquipmentMaintenances.Add(a);
        two.EquipmentMaintenances.Add(b);
        await one.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => two.SaveChangesAsync(TestContext.Current.CancellationToken));
        await using var read = fixture.CreateDbContext();
        Assert.False(await read.EquipmentMaintenances.AnyAsync(maintenance => maintenance.Id == b.Id,
            TestContext.Current.CancellationToken));
    }
}
