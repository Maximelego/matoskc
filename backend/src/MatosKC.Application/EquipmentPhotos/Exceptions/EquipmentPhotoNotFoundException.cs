namespace MatosKC.Application.EquipmentPhotos.Exceptions;

public sealed class EquipmentPhotoNotFoundException : Exception
{
    public EquipmentPhotoNotFoundException(Guid photoId)
        : base($"Equipment photo not found: (Id {photoId})")
    {
    }
}
