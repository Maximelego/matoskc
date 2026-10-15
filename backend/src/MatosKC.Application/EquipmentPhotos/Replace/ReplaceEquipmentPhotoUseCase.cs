namespace MatosKC.Application.EquipmentPhotos.Replace;

using MatosKC.Application.EquipmentPhotos.Exceptions;
using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Equipments;

public sealed class ReplaceEquipmentPhotoUseCase
{
    private readonly IEquipmentPhotoRepository PhotoRepository;
    private readonly IObjectStorage ObjectStorage;
    private readonly IImageContentValidator ImageValidator;

    public ReplaceEquipmentPhotoUseCase(
        IEquipmentPhotoRepository photoRepository,
        IObjectStorage objectStorage,
        IImageContentValidator imageValidator
    )
    {
        PhotoRepository = photoRepository;
        ObjectStorage = objectStorage;
        ImageValidator = imageValidator;
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

        await ObjectStorage.StoreAsync(
            photo.ObjectKey,
            validated,
            contentType,
            cancellationToken
        );

        photo.ReplaceMetadata(fileName, contentType, contentLength, DateTimeOffset.UtcNow);
        await PhotoRepository.UpdateAsync(photo, cancellationToken);

        return EquipmentPhotoResult.FromDomain(photo);
    }
}
