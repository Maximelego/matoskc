namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Inspections;
using MatosKC.Application.Maintenance;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Maintenance;
using MatosKC.Domain.Equipments;

public sealed class EquipmentCareWorkflowTests
{
    private static InspectionActor Admin => new(Guid.NewGuid(), Role.Admin, null);

    private static async Task<MatosKC.Domain.Entities.Inspection.Inspection> DefectiveDepartureAsync(WorkflowFake fake)
    {
        var inspection = await fake.DepartureAsync();
        var point = fake.Templates[0].Points[0];
        inspection.SetAnswer(point.Id, InspectionResult.NonCompliant, "Tuyau déchiré");
        inspection.AddAnswerPhoto(inspection.Answers[0].Id, Guid.NewGuid());
        inspection.Sign("Client", [1], fake.Clock.Now);
        await new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, fake.Actor);

        return inspection;
    }

    [Fact]
    public async Task Validation_CreatesDefectAndOccurrenceExactlyOnce()
    {
        var fake = new WorkflowFake();
        var inspection = await DefectiveDepartureAsync(fake);
        await new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, fake.Actor);

        var defect = Assert.Single(fake.Care.Defects);
        var occurrence = Assert.Single(fake.Care.Occurrences);
        Assert.Equal(inspection.Answers[0].Id, defect.InspectionAnswerId);
        Assert.Equal(defect.Id, occurrence.DefectId);
        Assert.Equal(inspection.Answers[0].PhotosIds, occurrence.PhotoIds);
        Assert.Equal(EquipmentDefectStatus.Open, defect.Status);
    }

    [Fact]
    public async Task CompliantValidation_DoesNotCreateDefect()
    {
        var fake = new WorkflowFake();
        await fake.DepartureAsync(true);
        Assert.Empty(fake.Care.Defects);
        Assert.Empty(fake.Care.Occurrences);
    }

    [Fact]
    public async Task NewTemplate_ReturnReviewsKnownDefectIndependentlyOfQuestions()
    {
        var fake = new WorkflowFake();
        await DefectiveDepartureAsync(fake);
        var defect = Assert.Single(fake.Care.Defects);
        var template = fake.AddTemplate("Freinage");
        var response = await new CreateReturnInspectionUseCase(fake.Context).ExecuteAsync(fake.Rentals[0].Id,
            new("Maxime"), fake.Actor);
        var inspection = fake.Inspections.Single(value => value.Id == response.Id);
        Assert.Equal("Etat extérieur", Assert.Single(response.KnownDefects).Description);
        await new SaveInspectionDraftUseCase(fake.Context).ExecuteAsync(inspection.Id,
            new("Maxime", [new(template.Points[0].Id, InspectionResult.Compliant, null)],
                [new(defect.Id, DefectReviewResult.Unchanged)]), fake.Actor);
        inspection.Sign("Client", [1], fake.Clock.Now);
        await new ValidateReturnInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, fake.Actor);
        Assert.Single(fake.Care.Defects);
        Assert.Single(fake.Care.Occurrences);
        Assert.Equal(EquipmentStatus.ToBeDecided, fake.Equipment.Status);
    }

    [Fact]
    public async Task Return_CompliantMeansNoNewDefect_AndStillRequiresReview()
    {
        var fake = new WorkflowFake();
        await DefectiveDepartureAsync(fake);
        var response = await new CreateReturnInspectionUseCase(fake.Context).ExecuteAsync(fake.Rentals[0].Id,
            new("Maxime"), fake.Actor);
        await new SaveInspectionDraftUseCase(fake.Context).ExecuteAsync(response.Id,
            new("Maxime", [new(fake.Templates[0].Points[0].Id, InspectionResult.Compliant, null)]), fake.Actor);
        var inspection = fake.Inspections.Single(value => value.Id == response.Id);
        inspection.Sign("Client", [1], fake.Clock.Now);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ValidateReturnInspectionUseCase(fake.Context).ExecuteAsync(response.Id, fake.Actor));
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
    }

    [Fact]
    public async Task Maintenance_ResolvesDefect_PreservesReportAndDoesNotReleaseEquipment()
    {
        var fake = new WorkflowFake();
        var inspection = await DefectiveDepartureAsync(fake);
        var report = inspection.ReportPdf;
        var defect = Assert.Single(fake.Care.Defects);
        var actor = Admin;
        var request = new RecordMaintenanceRequest(Guid.NewGuid(), fake.Clock.Now, "Atelier", "Tuyau remplacé", [defect.Id]);
        var useCase = new RecordMaintenanceUseCase(fake.Context);
        var response = await useCase.ExecuteAsync(fake.Equipment.Id, request, actor);
        var retry = await useCase.ExecuteAsync(fake.Equipment.Id, request, actor);

        Assert.Equal(response.Id, retry.Id);
        Assert.Single(fake.Care.Maintenances);
        Assert.Equal(EquipmentDefectStatus.Resolved, defect.Status);
        Assert.Equal(EquipmentStatus.Borrowed, fake.Equipment.Status);
        Assert.Equal(report, inspection.ReportPdf);
        var open = await new QueryEquipmentCareUseCase(fake.Context).ListDefectsAsync(
            fake.Equipment.Id, EquipmentDefectStatus.Open, 0, 50, fake.Actor);
        Assert.Empty(open.Items);
    }

    [Fact]
    public async Task Maintenance_RetryWithDifferentDataIsConflict_AndAgencyCannotCreate()
    {
        var fake = new WorkflowFake();
        var request = new RecordMaintenanceRequest(Guid.NewGuid(), fake.Clock.Now, "Atelier", "Graissage", []);
        var useCase = new RecordMaintenanceUseCase(fake.Context);
        var actor = Admin;
        await useCase.ExecuteAsync(fake.Equipment.Id, request, actor);

        var conflict = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.ExecuteAsync(fake.Equipment.Id, request with { WorkDescription = "Different" }, actor));
        Assert.Equal(409, conflict.StatusCode);
        var denied = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.ExecuteAsync(fake.Equipment.Id, request, fake.Actor));
        Assert.Equal(403, denied.StatusCode);
    }

    [Fact]
    public async Task Maintenance_RejectsForeignAndAlreadyResolvedDefects()
    {
        var fake = new WorkflowFake();
        var foreign = new EquipmentDefect(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Tuyau", "Déchiré", fake.Clock.Now, Guid.NewGuid(), "Maxime");
        fake.Care.Defects.Add(foreign);
        var useCase = new RecordMaintenanceUseCase(fake.Context);
        var request = new RecordMaintenanceRequest(Guid.NewGuid(), fake.Clock.Now, "Atelier", "Réparation", [foreign.Id]);
        var actor = Admin;
        var missing = await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.ExecuteAsync(fake.Equipment.Id, request, actor));
        Assert.Equal(404, missing.StatusCode);
        Assert.Empty(fake.Care.Maintenances);

        await DefectiveDepartureAsync(fake);
        var own = fake.Care.Defects.Single(defect => defect.EquipmentId == fake.Equipment.Id);
        var ownRequest = request with { Id = Guid.NewGuid(), ResolvedDefectIds = new[] { own.Id } };
        await useCase.ExecuteAsync(fake.Equipment.Id, ownRequest, actor);
        var resolved = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.ExecuteAsync(fake.Equipment.Id, ownRequest with { Id = Guid.NewGuid() }, actor));
        Assert.Equal(409, resolved.StatusCode);
    }

    [Fact]
    public async Task LegacyAnswerLinksAreRejected_UseIndependentReviews()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new SaveInspectionDraftUseCase(fake.Context).ExecuteAsync(inspection.Id,
                new("Maxime", [new(inspection.Answers[0].TemplatePointId, InspectionResult.NonCompliant,
                    "Défaut", Guid.NewGuid())]), fake.Actor));
        Assert.Equal(400, error.StatusCode);
    }

    [Fact]
    public async Task WorsenedReview_RechecksMaintenanceBeforeValidation()
    {
        var fake = new WorkflowFake();
        await DefectiveDepartureAsync(fake);
        var defect = Assert.Single(fake.Care.Defects);
        var response = await new CreateReturnInspectionUseCase(fake.Context).ExecuteAsync(fake.Rentals[0].Id, new("Maxime"), fake.Actor);
        var inspection = fake.Inspections.Single(value => value.Id == response.Id);
        inspection.SetAnswer(fake.Templates[0].Points[0].Id, InspectionResult.Compliant, null);
        inspection.ReviewDefect(defect.Id, DefectReviewResult.Worsened, "Plus grand");
        inspection.AddEvidencePhoto(inspection.KnownDefects[0].Id, Guid.NewGuid());
        inspection.Sign("Client", [1], fake.Clock.Now);
        await new RecordMaintenanceUseCase(fake.Context).ExecuteAsync(fake.Equipment.Id,
            new(Guid.NewGuid(), fake.Clock.Now, "Atelier", "Réparation", [defect.Id]), Admin);
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new ValidateReturnInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, fake.Actor));
        Assert.Equal(409, error.StatusCode);
        Assert.Single(fake.Care.Occurrences);
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
    }

    [Fact]
    public async Task BackdatedMaintenance_CannotHideALaterOccurrence()
    {
        var fake = new WorkflowFake();
        await DefectiveDepartureAsync(fake);
        var defect = Assert.Single(fake.Care.Defects);
        fake.Care.Occurrences.Add(new DefectOccurrence(Guid.NewGuid(), defect.Id, Guid.NewGuid(),
            Guid.NewGuid(), "Tuyau", "Encore déchiré", fake.Clock.Now.AddHours(1), [Guid.NewGuid()], Guid.NewGuid(), "Maxime"));
        fake.Clock.Now = fake.Clock.Now.AddHours(2);

        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new RecordMaintenanceUseCase(fake.Context).ExecuteAsync(fake.Equipment.Id,
                new(Guid.NewGuid(), fake.Clock.Now.AddHours(-2), "Atelier", "Réparation", [defect.Id]), Admin));

        Assert.Equal(409, error.StatusCode);
        Assert.Equal(EquipmentDefectStatus.Open, defect.Status);
        Assert.Empty(fake.Care.Maintenances);
    }

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(0, 101)]
    [InlineData(0, 0)]
    public async Task QueriesRejectInvalidPagination(int offset, int limit)
    {
        var fake = new WorkflowFake();
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new QueryEquipmentCareUseCase(fake.Context).ListDefectsAsync(fake.Equipment.Id, null, offset, limit, fake.Actor));
        Assert.Equal(400, error.StatusCode);
    }
}
