namespace MatosKC.Application.EquipmentPhotos.Delete;

using MatosKC.Application.EquipmentPhotos.Exceptions;
using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Equipments;

public sealed class DeleteEquipmentPhotoUseCase
{
    private readonly IEquipmentPhotoRepository PhotoRepository;
    private readonly IObjectStorage ObjectStorage;

    public DeleteEquipmentPhotoUseCase(
        IEquipmentPhotoRepository photoRepository,
        IObjectStorage objectStorage
    )
    {
        PhotoRepository = photoRepository;
        ObjectStorage = objectStorage;
    }

    public async Task ExecuteAsync(
        Guid equipmentId,
        Guid photoId,
        CancellationToken cancellationToken = default
    )
    {
        EquipmentPhoto? photo = await PhotoRepository.RetrieveByIdAsync(photoId, cancellationToken);

        if (photo is null || photo.EquipmentId != equipmentId)
        {
            throw new EquipmentPhotoNotFoundException(photoId);
        }

        await ObjectStorage.DeleteAsync(photo.ObjectKey, cancellationToken);
        await PhotoRepository.DeleteAsync(photo, cancellationToken);
    }
}
