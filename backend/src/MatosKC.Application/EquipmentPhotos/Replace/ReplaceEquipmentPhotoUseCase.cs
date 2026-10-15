namespace MatosKC.Application.EquipmentPhotos.Replace;

using MatosKC.Application.EquipmentPhotos.Exceptions;
using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Equipments;
using Microsoft.Extensions.Logging;

public sealed class ReplaceEquipmentPhotoUseCase
{
    private readonly IEquipmentPhotoRepository PhotoRepository;
    private readonly IObjectStorage ObjectStorage;
    private readonly IImageContentValidator ImageValidator;
    private readonly ILogger<ReplaceEquipmentPhotoUseCase>? Logger;

    public ReplaceEquipmentPhotoUseCase(
        IEquipmentPhotoRepository photoRepository,
        IObjectStorage objectStorage,
        IImageContentValidator imageValidator,
        ILogger<ReplaceEquipmentPhotoUseCase>? logger = null
    )
    {
        PhotoRepository = photoRepository;
        ObjectStorage = objectStorage;
        ImageValidator = imageValidator;
        Logger = logger;
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

        byte[] bytes = await MatosKC.Application.Files.ImageFileReader.ReadValidatedAsync(
            content, contentLength, contentType, ImageValidator, cancellationToken);
        using var validated = new MemoryStream(bytes, writable: false);

        string oldKey = photo.ObjectKey;
        string newKey = $"equipments/{equipmentId}/photos/{photoId}/{Guid.NewGuid()}";

        // Never overwrite the file currently referenced by PostgreSQL.
        await ObjectStorage.StoreAsync(newKey, validated, contentType, cancellationToken);

        photo.ReplaceContent(newKey, fileName, contentType, contentLength, DateTimeOffset.UtcNow);

        try
        {
            await PhotoRepository.UpdateAsync(photo, cancellationToken);
        }
        catch (Exception exception)
        {
            // Keep both objects: a disconnected client cannot know whether a commit succeeded.
            Logger?.LogWarning(exception,
                "Photo update failed or was interrupted; object {ObjectKey} requires reconciliation.", newKey);
            throw;
        }

        // The database now references the new object. Cleanup cannot undo that success.
        try
        {
            await ObjectStorage.DeleteAsync(oldKey, CancellationToken.None);
        }
        catch (Exception exception)
        {
            Logger?.LogWarning(exception, "Old equipment photo object {ObjectKey} requires cleanup.", oldKey);
        }

        return EquipmentPhotoResult.FromDomain(photo);
    }
}
