namespace MatosKC.Application.Tests.EquipmentPhotos;

using MatosKC.Application.EquipmentPhotos.Delete;
using MatosKC.Application.EquipmentPhotos.Download;
using MatosKC.Application.EquipmentPhotos.Exceptions;
using MatosKC.Application.EquipmentPhotos.List;
using MatosKC.Application.EquipmentPhotos.Replace;
using MatosKC.Application.EquipmentPhotos.Upload;
using MatosKC.Application.Equipments.Get.Exceptions;
using MatosKC.Application.Tests.Fakes;
using MatosKC.Domain.Equipments;

public sealed class EquipmentPhotoUseCaseTests
{
    [Fact]
    public async Task Upload_WithValidPng_ShouldStoreContentAndMetadata()
    {
        (Equipment equipment, FakeEquipmentRepository equipments) = CreateEquipmentRepository();
        var photos = new FakeEquipmentPhotoRepository();
        var storage = new FakeObjectStorage();
        var useCase = new UploadEquipmentPhotoUseCase(equipments, photos, storage);
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await useCase.ExecuteAsync(
            equipment.Id,
            content,
            " machine.png ",
            "image/png",
            content.Length
        );

        EquipmentPhoto photo = Assert.Single(photos.Photos);
        Assert.Equal(photo.Id, result.Id);
        Assert.Equal("machine.png", result.FileName);
        Assert.Contains(photo.ObjectKey, storage.ObjectKeys);
    }

    [Fact]
    public async Task Upload_WhenEquipmentDoesNotExist_ShouldThrow()
    {
        var useCase = new UploadEquipmentPhotoUseCase(
            new FakeEquipmentRepository(),
            new FakeEquipmentPhotoRepository(),
            new FakeObjectStorage()
        );
        await using var content = new MemoryStream([1]);

        await Assert.ThrowsAsync<EquipmentNotFoundException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), content, "photo.png", "image/png", 1));
    }

    [Theory]
    [InlineData("text/plain", 1)]
    [InlineData("image/png", 0)]
    [InlineData("image/jpeg", 10485761)]
    public async Task Upload_WithInvalidFile_ShouldThrow(
        string contentType,
        long contentLength
    )
    {
        (Equipment equipment, FakeEquipmentRepository equipments) = CreateEquipmentRepository();
        var useCase = new UploadEquipmentPhotoUseCase(
            equipments,
            new FakeEquipmentPhotoRepository(),
            new FakeObjectStorage()
        );
        await using var content = new MemoryStream([1]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(
                equipment.Id,
                content,
                "photo.bin",
                contentType,
                contentLength
            ));
    }

    [Fact]
    public async Task List_ShouldOnlyReturnPhotosForRequestedEquipment()
    {
        (Equipment equipment, FakeEquipmentRepository equipments) = CreateEquipmentRepository();
        var photos = new FakeEquipmentPhotoRepository();
        photos.Photos.Add(CreatePhoto(equipment.Id));
        photos.Photos.Add(CreatePhoto(Guid.NewGuid()));
        var useCase = new ListEquipmentPhotosUseCase(equipments, photos);

        var result = await useCase.ExecuteAsync(equipment.Id);

        Assert.Single(result);
        Assert.Equal(equipment.Id, result[0].EquipmentId);
    }

    [Fact]
    public async Task Download_ShouldReturnStoredContentAndFileName()
    {
        (Equipment equipment, _) = CreateEquipmentRepository();
        EquipmentPhoto photo = CreatePhoto(equipment.Id);
        var photos = new FakeEquipmentPhotoRepository();
        photos.Photos.Add(photo);
        var storage = new FakeObjectStorage();
        await using var source = new MemoryStream([4, 5, 6]);
        await storage.StoreAsync(photo.ObjectKey, source, photo.ContentType);
        var useCase = new DownloadEquipmentPhotoUseCase(photos, storage);

        var result = await useCase.ExecuteAsync(equipment.Id, photo.Id);

        await using (result.Object)
        {
            Assert.Equal(photo.OriginalFileName, result.FileName);
            Assert.Equal(3, result.Object.ContentLength);
        }
    }

    [Fact]
    public async Task Replace_ShouldOverwriteContentAndMetadata()
    {
        (Equipment equipment, _) = CreateEquipmentRepository();
        EquipmentPhoto photo = CreatePhoto(equipment.Id);
        var photos = new FakeEquipmentPhotoRepository();
        photos.Photos.Add(photo);
        var storage = new FakeObjectStorage();
        var useCase = new ReplaceEquipmentPhotoUseCase(photos, storage);
        await using var replacement = new MemoryStream([8, 9]);

        var result = await useCase.ExecuteAsync(
            equipment.Id,
            photo.Id,
            replacement,
            "replacement.webp",
            "image/webp",
            replacement.Length
        );

        Assert.Equal("replacement.webp", result.FileName);
        Assert.Equal("image/webp", result.ContentType);
        Assert.Contains(photo.ObjectKey, storage.ObjectKeys);
    }

    [Fact]
    public async Task Delete_ShouldRemoveObjectAndMetadata()
    {
        (Equipment equipment, _) = CreateEquipmentRepository();
        EquipmentPhoto photo = CreatePhoto(equipment.Id);
        var photos = new FakeEquipmentPhotoRepository();
        photos.Photos.Add(photo);
        var storage = new FakeObjectStorage();
        await using var source = new MemoryStream([1]);
        await storage.StoreAsync(photo.ObjectKey, source, photo.ContentType);
        var useCase = new DeleteEquipmentPhotoUseCase(photos, storage);

        await useCase.ExecuteAsync(equipment.Id, photo.Id);

        Assert.Empty(photos.Photos);
        Assert.Empty(storage.ObjectKeys);
    }

    [Fact]
    public async Task Delete_WithPhotoOwnedByAnotherEquipment_ShouldReturnNotFound()
    {
        EquipmentPhoto photo = CreatePhoto(Guid.NewGuid());
        var photos = new FakeEquipmentPhotoRepository();
        photos.Photos.Add(photo);
        var useCase = new DeleteEquipmentPhotoUseCase(photos, new FakeObjectStorage());

        await Assert.ThrowsAsync<EquipmentPhotoNotFoundException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), photo.Id));
    }

    private static (Equipment Equipment, FakeEquipmentRepository Repository)
        CreateEquipmentRepository()
    {
        var equipment = new Equipment(
            "Souffleur",
            Guid.NewGuid(),
            $"SN-{Guid.NewGuid():N}"
        );
        var repository = new FakeEquipmentRepository();
        repository.AddedEquipments.Add(equipment);
        return (equipment, repository);
    }

    private static EquipmentPhoto CreatePhoto(Guid equipmentId)
    {
        Guid photoId = Guid.NewGuid();
        return new EquipmentPhoto(
            photoId,
            equipmentId,
            $"equipments/{equipmentId}/photos/{photoId}",
            "photo.png",
            "image/png",
            1,
            DateTimeOffset.UtcNow
        );
    }
}
