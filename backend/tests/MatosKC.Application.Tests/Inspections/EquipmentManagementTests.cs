namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Equipments;
using MatosKC.Application.Equipments.List;
using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Equipments;

public sealed class EquipmentManagementTests
{
    private static InspectionActor Admin => new(Guid.NewGuid(), Role.Admin, null);

    [Fact]
    public async Task RenameDuringRental_AllowedButRecategorizationRejected()
    {
        var fake = new WorkflowFake();
        await fake.DepartureAsync();
        var useCase = new ManageEquipmentUseCase(fake.Context);
        var serial = fake.Equipment.SerialNumber;

        await useCase.UpdateAsync(fake.Equipment.Id,
            new("Nouveau nom", fake.Equipment.CategoryId), Admin);
        Assert.Equal("Nouveau nom", fake.Equipment.Name);
        Assert.Equal(serial, fake.Equipment.SerialNumber);

        var category = new EquipmentCategory("Other", null);
        fake.Categories.Seed(category);
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.UpdateAsync(fake.Equipment.Id, new("Other", category.Id), Admin));

        Assert.Equal(409, error.StatusCode);
        Assert.NotEqual(category.Id, fake.Equipment.CategoryId);
    }

    [Fact]
    public async Task AgencyCannotEdit_AndUnknownCategoryIsRejected()
    {
        var fake = new WorkflowFake();
        var useCase = new ManageEquipmentUseCase(fake.Context);
        var denied = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.UpdateAsync(fake.Equipment.Id, new("Name", fake.Equipment.CategoryId), fake.Actor));
        Assert.Equal(403, denied.StatusCode);

        var missing = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.UpdateAsync(fake.Equipment.Id, new("Name", Guid.NewGuid()), Admin));
        Assert.Equal(404, missing.StatusCode);
    }

    [Fact]
    public async Task CategoryUpdate_PreservesTemplatesAndRejectsDuplicateName()
    {
        var fake = new WorkflowFake();
        var template = Assert.Single(fake.Templates);
        var useCase = new ManageEquipmentUseCase(fake.Context);
        await useCase.UpdateCategoryAsync(fake.Equipment.CategoryId, new("Camions", "Description"), Admin);
        Assert.Equal(fake.Equipment.CategoryId, template.EquipmentCategoryId);

        fake.Categories.NameAlreadyExists = true;
        var duplicate = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            useCase.UpdateCategoryAsync(fake.Equipment.CategoryId, new("Duplicate", null), Admin));
        Assert.Equal(409, duplicate.StatusCode);
    }

    [Fact]
    public async Task List_EnrichesPhotosInOneBatch_AndLeavesMissingPhotoNull()
    {
        var fake = new WorkflowFake();
        var withoutPhoto = new Equipment("Without", fake.Equipment.CategoryId, "Other");
        fake.EquipmentRepository.AddedEquipments.Add(withoutPhoto);
        var thumbnails = new Thumbnails(fake.Equipment.Id);
        var result = await new ListEquipmentUseCase(fake.EquipmentRepository, thumbnails)
            .ExecuteAsync(new ListEquipmentsQuery(null, null, null), default);

        Assert.Equal(1, thumbnails.Calls);
        var withPhoto = result.Equipments.Single(dto => dto.Id == fake.Equipment.Id);
        Assert.Equal(thumbnails.PhotoId, withPhoto.PrimaryPhotoId);
        Assert.Contains($"/photos/{thumbnails.PhotoId}", withPhoto.ThumbnailUrl);
        Assert.Null(result.Equipments.Single(dto => dto.Id == withoutPhoto.Id).ThumbnailUrl);
    }

    private sealed class Thumbnails(Guid equipmentId) : IEquipmentThumbnailRepository
    {
        public int Calls { get; private set; }
        public Guid PhotoId { get; } = Guid.NewGuid();

        public Task<IReadOnlyDictionary<Guid, EquipmentThumbnail>> GetAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken ct)
        {
            Calls++;
            Assert.Equal(2, ids.Count);
            return Task.FromResult<IReadOnlyDictionary<Guid, EquipmentThumbnail>>(
                new Dictionary<Guid, EquipmentThumbnail> { [equipmentId] = new(PhotoId, DateTimeOffset.UtcNow) });
        }
    }
}
