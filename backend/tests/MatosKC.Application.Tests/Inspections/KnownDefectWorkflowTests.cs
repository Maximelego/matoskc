namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Inspections;
using MatosKC.Application.Maintenance;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Maintenance;
using MatosKC.Domain.Entities.Accounts;

public sealed class KnownDefectWorkflowTests
{
    private static EquipmentDefect Seed(WorkflowFake fake)
    {
        var defect = new EquipmentDefect(Guid.NewGuid(), fake.Equipment.Id, Guid.NewGuid(), Guid.NewGuid(),
            "Ancienne question supprimée", "Rayure", fake.Clock.Now.AddDays(-1));
        fake.Care.Defects.Add(defect);
        fake.Care.Occurrences.Add(new DefectOccurrence(Guid.NewGuid(), defect.Id, defect.InspectionId,
            defect.InspectionAnswerId!.Value, defect.Description, defect.Observation, defect.ReportedAt, [Guid.NewGuid()]));
        return defect;
    }

    [Fact]
    public async Task WorseningAndTwoNewFindingsCoexistWithoutDuplicatingKnownDefect()
    {
        var fake = new WorkflowFake(); var defect = Seed(fake);
        var inspection = await fake.DepartureAsync(); var point = inspection.Answers[0].TemplatePointId;
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var response = await new SaveInspectionDraftUseCase(fake.Context).ExecuteAsync(inspection.Id,
            new("Maxime", [new(point, InspectionResult.NonCompliant, null)],
                [new(defect.Id, DefectReviewResult.Worsened, "Rayure agrandie")],
                [new(first, point, "Impact"), new(second, point, "Fissure")]), fake.Actor);
        Assert.Equal(2, response.Findings.Count);
        inspection.AddEvidencePhoto(first, Guid.NewGuid()); inspection.AddEvidencePhoto(second, Guid.NewGuid());
        inspection.AddEvidencePhoto(inspection.KnownDefects[0].Id, Guid.NewGuid());
        inspection.Sign("Client", [1], fake.Clock.Now);
        var useCase = new ValidateDepartureInspectionUseCase(fake.Context);
        await useCase.ExecuteAsync(inspection.Id, fake.Actor);
        await useCase.ExecuteAsync(inspection.Id, fake.Actor);
        Assert.Equal(3, fake.Care.Defects.Count);
        Assert.Equal(4, fake.Care.Occurrences.Count);
        Assert.Equal(2, fake.Care.Occurrences.Count(value => value.DefectId == defect.Id));
        Assert.Equal("Rayure", inspection.KnownDefects[0].HistoricalObservation);
        Assert.Equal("Rayure agrandie", inspection.KnownDefects[0].Observation);
    }

    [Fact]
    public async Task MoreThanOnePageOfKnownDefectsAreAllPresented()
    {
        var fake = new WorkflowFake();
        for (var i = 0; i < 105; i++) Seed(fake);
        var inspection = await fake.DepartureAsync();
        Assert.Equal(105, inspection.KnownDefects.Count);
    }

    [Fact]
    public async Task NewDefectAfterSigningRequiresSaveReviewAndResign()
    {
        var fake = new WorkflowFake(); var inspection = await fake.DepartureAsync();
        var defect = Seed(fake);
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, fake.Actor));
        Assert.Equal(409, error.StatusCode);
        await new SaveInspectionDraftUseCase(fake.Context).ExecuteAsync(inspection.Id, new("Maxime", []), fake.Actor);
        Assert.Null(inspection.SignaturePng);
        Assert.Equal(defect.Id, Assert.Single(inspection.KnownDefects).DefectId);
    }

    [Fact]
    public async Task PartialUpdatesPreserveFindingsAndPhotos_ExplicitRemovalInvalidatesSignature()
    {
        var fake = new WorkflowFake(); var inspection = await fake.DepartureAsync(); var point = inspection.Answers[0].TemplatePointId;
        var id = Guid.NewGuid(); var save = new SaveInspectionDraftUseCase(fake.Context);
        await save.ExecuteAsync(inspection.Id, new("Maxime", [new(point, InspectionResult.NonCompliant, null)],
            Findings: [new(id, point, "Impact")]), fake.Actor);
        inspection.AddEvidencePhoto(id, Guid.NewGuid());
        await save.ExecuteAsync(inspection.Id, new("Maxime", []), fake.Actor);
        Assert.Single(Assert.Single(inspection.Findings).PhotoIds);
        inspection.Sign("Client", [1], fake.Clock.Now);
        await save.ExecuteAsync(inspection.Id, new("Maxime", [new(point, InspectionResult.Compliant, null)],
            RemovedFindingIds: [id]), fake.Actor);
        Assert.Empty(inspection.Findings); Assert.Null(inspection.SignaturePng);
    }

    [Fact]
    public async Task MaintenanceDoesNotRewriteSnapshot_AndCheckRequiredDoesNotResolve()
    {
        var fake = new WorkflowFake(); var defect = Seed(fake); var inspection = await fake.DepartureAsync();
        inspection.ReviewDefect(defect.Id, DefectReviewResult.MaintenanceCheckRequired, "Semble disparu");
        inspection.Sign("Client", [1], fake.Clock.Now);
        await new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, fake.Actor);
        Assert.Equal(EquipmentDefectStatus.Open, defect.Status);
        var snapshot = inspection.KnownDefects[0]; var report = inspection.ReportPdf;
        await new RecordMaintenanceUseCase(fake.Context).ExecuteAsync(fake.Equipment.Id,
            new(Guid.NewGuid(), fake.Clock.Now, "Atelier", "Réparation", [defect.Id]), new(Guid.NewGuid(), Role.Admin, null));
        Assert.Equal("Rayure", snapshot.HistoricalObservation);
        Assert.Equal(DefectReviewResult.MaintenanceCheckRequired, snapshot.Result);
        Assert.Equal(report, inspection.ReportPdf);
    }

    [Fact]
    public async Task LaterOccurrenceRequiresReviewingUpdatedEvidence()
    {
        var fake = new WorkflowFake(); var defect = Seed(fake); var inspection = await fake.DepartureAsync();
        inspection.ReviewDefect(defect.Id, DefectReviewResult.Unchanged, null);
        inspection.Sign("Client", [1], fake.Clock.Now);
        fake.Care.Occurrences.Add(new DefectOccurrence(Guid.NewGuid(), defect.Id, Guid.NewGuid(), Guid.NewGuid(),
            defect.Description, "Aggravation concurrente", fake.Clock.Now, [Guid.NewGuid()]));
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, fake.Actor));
        Assert.Equal(409, error.StatusCode);
        await new SaveInspectionDraftUseCase(fake.Context).ExecuteAsync(inspection.Id, new("Maxime", []), fake.Actor);
        Assert.Equal("Aggravation concurrente", inspection.KnownDefects[0].HistoricalObservation);
        Assert.Null(inspection.KnownDefects[0].Result);
        Assert.Null(inspection.SignaturePng);
    }

    [Fact]
    public async Task EvidencePhotosAreScopedAndDetachedPhotosAreNotDownloadable()
    {
        var fake = new WorkflowFake(); var defect = Seed(fake); var inspection = await fake.DepartureAsync();
        var findingId = Guid.NewGuid();
        inspection.SetFinding(findingId, inspection.Answers[0].TemplatePointId, "Impact");
        var photos = new ManageInspectionPhotosUseCase(fake.Context);
        foreach (var target in new[] { findingId, inspection.KnownDefects[0].Id })
        {
            var media = await photos.UploadAsync(inspection.Id, target, new MemoryStream([1]), "photo.png", "image/png", 1, fake.Actor);
            Assert.Null(media.AnswerId);
            Assert.True(media.FindingId == target || media.DefectReviewId == target);
            var stored = await photos.DownloadAsync(inspection.Id, media.Id, fake.Actor);
            stored.Content.Dispose();
            var foreign = new InspectionActor(Guid.NewGuid(), Role.Agency, 33, Guid.NewGuid());
            var denied = await Assert.ThrowsAsync<InspectionOperationException>(() => photos.DownloadAsync(inspection.Id, media.Id, foreign));
            Assert.Equal(403, denied.StatusCode);
            await photos.RemoveAsync(inspection.Id, media.Id, fake.Actor);
            await Assert.ThrowsAsync<InspectionOperationException>(() => photos.DownloadAsync(inspection.Id, media.Id, fake.Actor));
        }
    }

    [Fact]
    public async Task HistoricalPhotoCanBeReadThroughCurrentInspectionButCannotBeRemoved()
    {
        var fake = new WorkflowFake();
        var departure = await fake.DepartureAsync();
        var point = departure.Answers[0].TemplatePointId;
        departure.SetAnswer(point, InspectionResult.NonCompliant, "Rayure");
        var photos = new ManageInspectionPhotosUseCase(fake.Context);
        var media = await photos.UploadAsync(departure.Id, departure.Answers[0].Id,
            new MemoryStream([1]), "photo.png", "image/png", 1, fake.Actor);
        departure.Sign("Client", [1], fake.Clock.Now);
        await new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(departure.Id, fake.Actor);
        var current = await new CreateReturnInspectionUseCase(fake.Context).ExecuteAsync(departure.RentalId, new("Maxime"), fake.Actor);
        var stored = await photos.DownloadAsync(current.Id, media.Id, fake.Actor);
        stored.Content.Dispose();
        var denied = await Assert.ThrowsAsync<InspectionOperationException>(() => photos.RemoveAsync(current.Id, media.Id, fake.Actor));
        Assert.Equal(404, denied.StatusCode);
    }

    [Fact]
    public async Task UnknownAndDuplicateReviewsAreRejected()
    {
        var fake = new WorkflowFake(); var defect = Seed(fake); var inspection = await fake.DepartureAsync();
        var save = new SaveInspectionDraftUseCase(fake.Context);
        foreach (var reviews in new SaveDefectReviewRequest[][]
        {
            [new(Guid.NewGuid(), DefectReviewResult.Unchanged)],
            [new(defect.Id, DefectReviewResult.Unchanged), new(defect.Id, DefectReviewResult.Unchanged)],
            [new(defect.Id, (DefectReviewResult)99)]
        })
        {
            var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
                save.ExecuteAsync(inspection.Id, new("Maxime", [], reviews), fake.Actor));
            Assert.Equal(400, error.StatusCode);
        }
    }
}
