namespace MatosKC.Application.EquipmentPhotos.Download;

using MatosKC.Application.EquipmentPhotos.Exceptions;
using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Equipments;

public sealed class DownloadEquipmentPhotoUseCase
{
    private readonly IEquipmentPhotoRepository PhotoRepository;
    private readonly IObjectStorage ObjectStorage;

    public DownloadEquipmentPhotoUseCase(
        IEquipmentPhotoRepository photoRepository,
        IObjectStorage objectStorage
    )
    {
        PhotoRepository = photoRepository;
        ObjectStorage = objectStorage;
    }

    public async Task<DownloadEquipmentPhotoResult> ExecuteAsync(
        Guid equipmentId,
        Guid photoId,
        CancellationToken cancellationToken = default
    )
    {
        EquipmentPhoto photo = await RetrievePhotoAsync(equipmentId, photoId, cancellationToken);
        StoredObject storedObject = await ObjectStorage.RetrieveAsync(
            photo.ObjectKey,
            cancellationToken
        ) ?? throw new EquipmentPhotoNotFoundException(photoId);

        return new DownloadEquipmentPhotoResult(storedObject, photo.OriginalFileName);
    }

    private async Task<EquipmentPhoto> RetrievePhotoAsync(
        Guid equipmentId,
        Guid photoId,
        CancellationToken cancellationToken
    )
    {
        EquipmentPhoto? photo = await PhotoRepository.RetrieveByIdAsync(
            photoId,
            cancellationToken
        );

        if (photo is null || photo.EquipmentId != equipmentId)
        {
            throw new EquipmentPhotoNotFoundException(photoId);
        }

        return photo;
    }
}
