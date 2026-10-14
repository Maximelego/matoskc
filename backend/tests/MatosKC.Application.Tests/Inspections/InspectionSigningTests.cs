namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Accounts;

public sealed class InspectionSigningTests
{
    [Fact]
    public async Task Signing_RequiresAcceptanceAndOperatorRights()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var useCase = new SignInspectionUseCase(fake.Context);
        await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.ExecuteAsync(inspection.Id,
            new("Client", "AQ==", false), fake.Actor));
        await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.ExecuteAsync(inspection.Id,
            new("Client", "AQ==", true), new(Guid.NewGuid(), Role.Admin, 83)));
        await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.ExecuteAsync(inspection.Id,
            new("Client", "not-base64", true), fake.Actor));
        var signed = await useCase.ExecuteAsync(inspection.Id, new(" Client ", "AQ==", true), fake.Actor);
        Assert.Equal("Client", signed.SignerName);
        Assert.Equal(fake.Clock.Now, signed.SignedAt);
    }

    [Fact]
    public async Task Validation_GeneratesFinalReportAndRetryPreservesIt()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var useCase = new ValidateDepartureInspectionUseCase(fake.Context);
        var validated = await useCase.ExecuteAsync(inspection.Id, fake.Actor);
        var download = new DownloadInspectionReportUseCase(fake.Context);
        var bytes = await download.ExecuteAsync(inspection.Id, fake.Actor);
        Assert.True(validated.HasReport);
        fake.Clock.Now = fake.Clock.Now.AddDays(1);
        await useCase.ExecuteAsync(inspection.Id, fake.Actor);
        Assert.Equal(bytes, await download.ExecuteAsync(inspection.Id, fake.Actor));
        await Assert.ThrowsAsync<InspectionOperationException>(() => download.ExecuteAsync(inspection.Id,
            new(Guid.NewGuid(), Role.Agency, 33, Guid.NewGuid())));
    }

    [Fact]
    public async Task Templates_CreateNewImmutableTemplateForCategoryAndRejectAgencyAndUnknownCategory()
    {
        var fake = new WorkflowFake();
        var useCase = new ManageInspectionTemplatesUseCase(fake.Context);
        var request = new CreateTemplateRequest(fake.Equipment.CategoryId,
            [new("Premier", null), new("Second", null)]);
        await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.CreateAsync(request, fake.Actor));
        var admin = new InspectionActor(Guid.NewGuid(), Role.Admin, null);
        var created = await useCase.CreateAsync(request, admin);
        Assert.Equal(fake.Equipment.CategoryId, created.EquipmentCategoryId);
        Assert.Equal(new[] { 0, 1 }, created.Points.Select(point => point.Position));
        Assert.Equal(created.Id, (await useCase.GetLatestAsync(fake.Equipment.CategoryId)).Id);
        Assert.Equal("Etat extérieur", (await useCase.GetAsync(fake.Templates[0].Id)).Points[0].Description);
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.CreateAsync(request with { EquipmentCategoryId = Guid.NewGuid() }, admin));
        Assert.Equal(404, error.StatusCode);
    }
}
