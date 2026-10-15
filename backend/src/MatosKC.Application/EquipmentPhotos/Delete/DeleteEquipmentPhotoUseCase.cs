namespace MatosKC.Application.EquipmentPhotos.Delete;

using MatosKC.Application.EquipmentPhotos.Exceptions;
using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Equipments;
using Microsoft.Extensions.Logging;

public sealed class DeleteEquipmentPhotoUseCase
{
    private readonly IEquipmentPhotoRepository PhotoRepository;
    private readonly IObjectStorage ObjectStorage;
    private readonly ILogger<DeleteEquipmentPhotoUseCase>? Logger;

    public DeleteEquipmentPhotoUseCase(
        IEquipmentPhotoRepository photoRepository,
        IObjectStorage objectStorage,
        ILogger<DeleteEquipmentPhotoUseCase>? logger = null
    )
    {
        PhotoRepository = photoRepository;
        ObjectStorage = objectStorage;
        Logger = logger;
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

        await PhotoRepository.DeleteAsync(photo, cancellationToken);

        try
        {
            await ObjectStorage.DeleteAsync(photo.ObjectKey, CancellationToken.None);
        }
        catch (Exception exception)
        {
            Logger?.LogWarning(exception, "Deleted equipment photo object {ObjectKey} requires cleanup.", photo.ObjectKey);
        }
    }
}
