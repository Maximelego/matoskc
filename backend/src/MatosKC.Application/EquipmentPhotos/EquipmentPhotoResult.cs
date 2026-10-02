namespace MatosKC.Application.EquipmentPhotos;

using MatosKC.Domain.Equipments;

public sealed record EquipmentPhotoResult(
    Guid Id,
    Guid EquipmentId,
    string FileName,
    string ContentType,
    long ContentLength,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc
)
{
    public static EquipmentPhotoResult FromDomain(EquipmentPhoto photo)
    {
        return new(
            photo.Id,
            photo.EquipmentId,
            photo.OriginalFileName,
            photo.ContentType,
            photo.ContentLength,
            photo.CreatedAtUtc,
            photo.UpdatedAtUtc
        );
    }
}
