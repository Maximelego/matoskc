namespace MatosKC.Application.EquipmentPhotos.Replace;

using MatosKC.Application.EquipmentPhotos.Exceptions;
using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Equipments;

public sealed class ReplaceEquipmentPhotoUseCase
{
    private readonly IEquipmentPhotoRepository PhotoRepository;
    private readonly IObjectStorage ObjectStorage;

    public ReplaceEquipmentPhotoUseCase(
        IEquipmentPhotoRepository photoRepository,
        IObjectStorage objectStorage
    )
    {
        PhotoRepository = photoRepository;
        ObjectStorage = objectStorage;
    }

    public async Task<EquipmentPhotoResult> ExecuteAsync(
        Guid equipmentId,
        Guid photoId,
        Stream content,
        string fileName,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default
    )
    {
        EquipmentPhotoFileValidator.Validate(fileName, contentType, contentLength);
        EquipmentPhoto? photo = await PhotoRepository.RetrieveByIdAsync(photoId, cancellationToken);

        if (photo is null || photo.EquipmentId != equipmentId)
        {
            throw new EquipmentPhotoNotFoundException(photoId);
        }

        await ObjectStorage.StoreAsync(
            photo.ObjectKey,
            content,
            contentType,
            cancellationToken
        );

        photo.ReplaceMetadata(fileName, contentType, contentLength, DateTimeOffset.UtcNow);
        await PhotoRepository.UpdateAsync(photo, cancellationToken);

        return EquipmentPhotoResult.FromDomain(photo);
    }
}
