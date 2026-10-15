namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Accounts;

public sealed class TemplatePhotoTests
{
    private static InspectionActor Admin => new(Guid.NewGuid(), Role.Admin, 83);

    [Fact]
    public async Task Upload_DownloadAndReuseInImmutableTemplates()
    {
        var fake = new WorkflowFake();
        var useCase = new ManageInspectionTemplatePhotosUseCase(fake.Context);
        var photo = await useCase.UploadAsync(new MemoryStream([1, 2, 3]), "guide.png", "image/png", 3, Admin);
        var metadata = Assert.Single(fake.TemplatePhotos);
        Assert.Equal(metadata.Id, photo.Id);
        Assert.Equal(1, fake.ImageValidator.Calls);
        await using var downloaded = await useCase.DownloadAsync(photo.Id, fake.Actor);
        Assert.Equal(3, downloaded.ContentLength);
        var templates = new ManageInspectionTemplatesUseCase(fake.Context);
        var request = new CreateTemplateRequest(fake.Equipment.CategoryId,
            [new("Contrôle", "Vérifier le capot", photo.Id)]);
        var first = await templates.CreateAsync(request, Admin);
        var second = await templates.CreateAsync(request, Admin);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(photo.Id, first.Points[0].PhotoId);
        Assert.Equal(photo.Id, second.Points[0].PhotoId);
        Assert.Equal("Vérifier le capot", first.Points[0].DescriptionDetail);
        Assert.Contains(metadata.ObjectKey, fake.Storage.ObjectKeys);
    }

    [Fact]
    public async Task Upload_RejectsAgencyAndMalformedDataBeforeWriting()
    {
        var fake = new WorkflowFake();
        var useCase = new ManageInspectionTemplatePhotosUseCase(fake.Context);
        await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.UploadAsync(
            new MemoryStream([1]), "guide.png", "image/png", 1, fake.Actor));
        fake.ImageValidator.Reject = true;
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.UploadAsync(
            new MemoryStream([1]), "guide.png", "image/png", 1, Admin));
        Assert.Empty(fake.Storage.ObjectKeys);
        Assert.Empty(fake.TemplatePhotos);
    }

    [Fact]
    public async Task Upload_CleansStoredObjectWhenCommitFails()
    {
        var fake = new WorkflowFake { FailCommit = true };
        var useCase = new ManageInspectionTemplatePhotosUseCase(fake.Context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.UploadAsync(
            new MemoryStream([1]), "guide.png", "image/png", 1, Admin));
        Assert.Empty(fake.Storage.ObjectKeys);
    }

    [Fact]
    public async Task Template_RejectsUnknownAndUnavailableIllustrations()
    {
        var fake = new WorkflowFake();
        var templates = new ManageInspectionTemplatesUseCase(fake.Context);
        var missing = new CreateTemplateRequest(fake.Equipment.CategoryId,
            [new("Contrôle", "Détail", Guid.NewGuid())]);
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() => templates.CreateAsync(missing, Admin));
        Assert.Equal(400, error.StatusCode);
        var photo = await new ManageInspectionTemplatePhotosUseCase(fake.Context).UploadAsync(
            new MemoryStream([1]), "guide.png", "image/png", 1, Admin);
        await fake.Storage.DeleteAsync(fake.TemplatePhotos[0].ObjectKey, default);
        var unavailable = missing with { Points = [new("Contrôle", "Détail", photo.Id)] };
        error = await Assert.ThrowsAsync<InspectionOperationException>(() => templates.CreateAsync(unavailable, Admin));
        Assert.Equal(409, error.StatusCode);
        Assert.Single(fake.Templates);
    }

    [Fact]
    public async Task InspectionUpload_RejectsInvalidImageWithoutRemovingExistingSignature()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        fake.ImageValidator.Reject = true;
        await Assert.ThrowsAsync<ArgumentException>(() => new ManageInspectionPhotosUseCase(fake.Context).UploadAsync(
            inspection.Id, inspection.Answers[0].Id, new MemoryStream([1]), "bad.png", "image/png", 1, fake.Actor));
        Assert.NotNull(inspection.SignaturePng);
        Assert.Empty(inspection.Answers[0].PhotosIds);
        Assert.Empty(fake.Storage.ObjectKeys);
    }
}
