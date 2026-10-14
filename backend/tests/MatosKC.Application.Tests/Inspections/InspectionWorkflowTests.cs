namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;

public sealed class InspectionWorkflowTests
{
    [Fact]
    public async Task Departure_CreatesCommandClientRentalAndDraftInOneCommit()
    {
        var fake = new WorkflowFake();
        var response = await new CreateDepartureInspectionUseCase(fake.Context).ExecuteAsync(fake.Request, fake.Actor);

        Assert.Equal(InspectionStatus.Draft, response.Status);
        Assert.Equal(fake.Clock.Now, response.StartedAt);
        Assert.Equal(fake.Actor.AccountId, response.CreatedByAccountId);
        Assert.Equal(RentalStatus.Draft, Assert.Single(fake.Rentals).Status);
        Assert.Equal("001234", Assert.Single(fake.Clients).CommandNumber.Value);
        Assert.Equal(EquipmentStatus.Available, fake.Equipment.Status);
        Assert.Equal(1, fake.Commits);
    }

    [Theory]
    [InlineData(EquipmentStatus.Borrowed)]
    [InlineData(EquipmentStatus.ToBeDecided)]
    [InlineData(EquipmentStatus.Unavailable)]
    [InlineData(EquipmentStatus.Maintenance)]
    [InlineData(EquipmentStatus.Decommissioned)]
    public async Task Departure_RejectsUnavailableEquipmentWithoutWriting(EquipmentStatus status)
    {
        var fake = new WorkflowFake();

        if (status is EquipmentStatus.ToBeDecided or EquipmentStatus.Unavailable or EquipmentStatus.Maintenance)
        {
            fake.Equipment.ChangeStatus(EquipmentStatus.Borrowed);
            fake.Equipment.ChangeStatus(EquipmentStatus.ToBeDecided);

            if (status != EquipmentStatus.ToBeDecided)
            {
                fake.Equipment.ChangeStatus(EquipmentStatus.Unavailable);
            }

            if (status == EquipmentStatus.Maintenance)
            {
                fake.Equipment.ChangeStatus(EquipmentStatus.Maintenance);
            }
        }
        else
        {
            fake.Equipment.ChangeStatus(status);
        }

        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new CreateDepartureInspectionUseCase(fake.Context).ExecuteAsync(fake.Request, fake.Actor));

        Assert.Equal(409, error.StatusCode);
        Assert.Empty(fake.Clients);
        Assert.Equal(0, fake.Commits);
    }

    [Fact]
    public async Task Departure_RejectsDuplicateOpenRentalAndMissingTemplate()
    {
        var fake = new WorkflowFake();
        await fake.DepartureAsync();

        var duplicate = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new CreateDepartureInspectionUseCase(fake.Context).ExecuteAsync(fake.Request, fake.Actor));
        Assert.Equal(409, duplicate.StatusCode);
        Assert.Single(fake.Rentals);

        var missing = new WorkflowFake();
        missing.Templates.Clear();
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new CreateDepartureInspectionUseCase(missing.Context).ExecuteAsync(missing.Request, missing.Actor));
        Assert.Equal(409, error.StatusCode);
        Assert.Empty(missing.Clients);
    }

    [Theory]
    [InlineData(Role.Admin, 83)]
    [InlineData(Role.Agency, 33)]
    public async Task Departure_RejectsUnauthorizedRoleAndForeignAgency(Role role, int code)
    {
        var fake = new WorkflowFake();
        var actor = new InspectionActor(Guid.NewGuid(), role, code);
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new CreateDepartureInspectionUseCase(fake.Context).ExecuteAsync(fake.Request, actor));

        Assert.Equal(403, error.StatusCode);
        Assert.Empty(fake.Rentals);
    }

    [Fact]
    public async Task SaveDraft_AllowsIncompleteAnswersAndPreservesPhotosOnUpdate()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var pointId = fake.Templates[0].Points[0].Id;
        var useCase = new SaveInspectionDraftUseCase(fake.Context);
        await useCase.ExecuteAsync(inspection.Id,
            new SaveDraftRequest("Maxime", [new(pointId, InspectionResult.NonCompliant, null)]), fake.Actor);
        var photo = await new ManageInspectionPhotosUseCase(fake.Context).UploadAsync(
            inspection.Id, inspection.Answers[0].Id, new MemoryStream([1, 2, 3]),
            "photo.png", "image/png", 3, fake.Actor);

        var updated = await useCase.ExecuteAsync(inspection.Id,
            new SaveDraftRequest("Autre", [new(pointId, InspectionResult.NonCompliant, "Défaut")]), fake.Actor);

        Assert.Equal(photo.Id, Assert.Single(updated.Answers[0].PhotoIds));
        Assert.Equal("Autre", updated.OperatorFirstName);
        Assert.Equal(InspectionStatus.Draft, updated.Status);
    }

    [Fact]
    public async Task SaveDraft_RejectsForeignDuplicateAndInvalidResultsBeforeChangingOperator()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var point = fake.Templates[0].Points[0].Id;
        var useCase = new SaveInspectionDraftUseCase(fake.Context);

        foreach (var answers in new IReadOnlyList<SaveAnswerRequest>[]
        {
            [new(Guid.NewGuid(), InspectionResult.Compliant, null)],
            [new(point, InspectionResult.Compliant, null), new(point, InspectionResult.Compliant, null)],
            [new(point, null, null)],
            [new(point, (InspectionResult)99, null)]
        })
        {
            var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
                useCase.ExecuteAsync(inspection.Id, new SaveDraftRequest("Changed", answers), fake.Actor));
            Assert.Equal(400, error.StatusCode);
            Assert.Equal("Maxime", inspection.OperatorFirstName);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Return_UsesLatestTemplateAndCompletesRentalWithExpectedEquipmentStatus(bool defective)
    {
        var fake = new WorkflowFake();
        var departure = await fake.DepartureAsync(validate: true);
        Assert.Equal(EquipmentStatus.Borrowed, fake.Equipment.Status);
        var latest = fake.AddTemplate();
        fake.Clock.Now = fake.Clock.Now.AddDays(1);
        var created = await new CreateReturnInspectionUseCase(fake.Context).ExecuteAsync(
            departure.RentalId, new CreateReturnRequest("Maxime"), fake.Actor);
        Assert.Equal(latest.Id, created.TemplateId);
        Assert.NotEqual(departure.TemplateId, created.TemplateId);
        var returnInspection = fake.Inspections.Single(value => value.Id == created.Id);
        returnInspection.SetAnswer(latest.Points[0].Id,
            defective ? InspectionResult.NonCompliant : InspectionResult.Compliant, defective ? "Défaut" : null);

        if (defective)
        {
            await new ManageInspectionPhotosUseCase(fake.Context).UploadAsync(returnInspection.Id,
                returnInspection.Answers[0].Id, new MemoryStream([1]), "photo.png", "image/png", 1, fake.Actor);
        }

        var useCase = new ValidateReturnInspectionUseCase(fake.Context);
        var validated = await useCase.ExecuteAsync(returnInspection.Id, fake.Actor);
        Assert.Equal(RentalStatus.Completed, fake.Rentals[0].Status);
        Assert.Equal(defective ? EquipmentStatus.ToBeDecided : EquipmentStatus.Available, fake.Equipment.Status);
        fake.Clock.Now = fake.Clock.Now.AddDays(1);
        var retried = await useCase.ExecuteAsync(returnInspection.Id, fake.Actor);
        Assert.Equal(validated.ValidatedAt, retried.ValidatedAt);
    }

    [Fact]
    public async Task Return_RejectsUnconfirmedDepartureAndResumesExistingDraft()
    {
        var fake = new WorkflowFake();
        var departure = await fake.DepartureAsync();
        var useCase = new CreateReturnInspectionUseCase(fake.Context);
        await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.ExecuteAsync(departure.RentalId, new CreateReturnRequest("Maxime"), fake.Actor));
        await new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(departure.Id, fake.Actor);
        var first = await useCase.ExecuteAsync(departure.RentalId, new CreateReturnRequest("Maxime"), fake.Actor);
        fake.AddTemplate();
        var second = await useCase.ExecuteAsync(departure.RentalId, new CreateReturnRequest("Autre"), fake.Actor);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.TemplateId, second.TemplateId);
    }

    [Fact]
    public async Task Queries_RestrictAgencyListingAndForeignRentalAccess()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var query = new QueryInspectionsUseCase(fake.Context);
        var other = new InspectionActor(Guid.NewGuid(), Role.Agency, 33);

        await Assert.ThrowsAsync<InspectionOperationException>(() => query.GetAsync(inspection.Id, other));
        await Assert.ThrowsAsync<InspectionOperationException>(() => query.ListAsync(fake.Actor, null, null, null));
        Assert.Single(await query.ListAsync(fake.Actor, inspection.RentalId, null, null));
        Assert.Single(await query.ListAsync(new(Guid.NewGuid(), Role.Admin, 33), null, null, null));
    }

    [Fact]
    public async Task Photos_RejectInvalidFilesAndCleanStorageIfCommitFails()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var useCase = new ManageInspectionPhotosUseCase(fake.Context);
        await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.UploadAsync(
            inspection.Id, inspection.Answers[0].Id, new MemoryStream([1]), "bad.txt", "text/plain", 1, fake.Actor));
        Assert.Empty(fake.Storage.ObjectKeys);
        fake.FailCommit = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.UploadAsync(
            inspection.Id, inspection.Answers[0].Id, new MemoryStream([1]), "photo.png", "image/png", 1, fake.Actor));
        Assert.Empty(fake.Storage.ObjectKeys);
    }

    [Fact]
    public async Task Photos_DownloadAndDetachWhileDraftButRejectChangesAfterValidation()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var useCase = new ManageInspectionPhotosUseCase(fake.Context);
        var media = await useCase.UploadAsync(inspection.Id, inspection.Answers[0].Id,
            new MemoryStream([1, 2]), "photo.png", "image/png", 2, fake.Actor);
        var stored = await useCase.DownloadAsync(inspection.Id, media.Id, fake.Actor);
        Assert.Equal(2, stored.ContentLength);
        stored.Content.Dispose();
        await useCase.RemoveAsync(inspection.Id, media.Id, fake.Actor);
        await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.DownloadAsync(inspection.Id, media.Id, fake.Actor));
        await new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, fake.Actor);
        await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.UploadAsync(
            inspection.Id, inspection.Answers[0].Id, new MemoryStream([1]), "photo.png", "image/png", 1, fake.Actor));
    }

    [Fact]
    public async Task Administrator_CanReleaseToBeDecidedEquipmentButCannotSimulateBorrowing()
    {
        var fake = new WorkflowFake();
        fake.Equipment.ChangeStatus(EquipmentStatus.Borrowed);
        fake.Equipment.ChangeStatus(EquipmentStatus.ToBeDecided);
        var actor = new InspectionActor(Guid.NewGuid(), Role.Admin, 83);
        var useCase = new ChangeEquipmentStatusUseCase(fake.Context);
        await useCase.ExecuteAsync(fake.Equipment.Id, EquipmentStatus.Available, actor);
        Assert.Equal(EquipmentStatus.Available, fake.Equipment.Status);
        await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.ExecuteAsync(fake.Equipment.Id, EquipmentStatus.Borrowed, actor));
        await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.ExecuteAsync(fake.Equipment.Id, EquipmentStatus.Decommissioned, fake.Actor));
    }
}
