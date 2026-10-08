namespace MatosKC.Application.EquipmentPhotos.Upload;

using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Application.Equipments.Get.Exceptions;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Equipments;

public sealed class UploadEquipmentPhotoUseCase
{
    private readonly IEquipmentRepository EquipmentRepository;
    private readonly IEquipmentPhotoRepository PhotoRepository;
    private readonly IObjectStorage ObjectStorage;

    public UploadEquipmentPhotoUseCase(
        IEquipmentRepository equipmentRepository,
        IEquipmentPhotoRepository photoRepository,
        IObjectStorage objectStorage
    )
    {
        EquipmentRepository = equipmentRepository;
        PhotoRepository = photoRepository;
        ObjectStorage = objectStorage;
    }

    public async Task<EquipmentPhotoResult> ExecuteAsync(
        Guid equipmentId,
        Stream content,
        string fileName,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default
    )
    {
        EquipmentPhotoFileValidator.Validate(fileName, contentType, contentLength);

        if (!await EquipmentRepository.ExistsByIdAsync(equipmentId, cancellationToken))
        {
            throw new EquipmentNotFoundException(equipmentId);
        }

        Guid photoId = Guid.NewGuid();
        string objectKey = $"equipments/{equipmentId}/photos/{photoId}";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var photo = new EquipmentPhoto(
            photoId,
            equipmentId,
            objectKey,
            fileName,
            contentType,
            contentLength,
            now
        );

        await ObjectStorage.StoreAsync(
            objectKey,
            content,
            photo.ContentType,
            cancellationToken
        );

        try
        {
            await PhotoRepository.AddAsync(photo, cancellationToken);
        }
        catch
        {
            await ObjectStorage.DeleteAsync(objectKey, cancellationToken);
            throw;
        }

        return EquipmentPhotoResult.FromDomain(photo);
    }
}
