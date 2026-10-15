namespace MatosKC.Application.EquipmentPhotos.Ports;

public sealed record EquipmentThumbnail(Guid PhotoId, DateTimeOffset UpdatedAtUtc);

public interface IEquipmentThumbnailRepository
{
    Task<IReadOnlyDictionary<Guid, EquipmentThumbnail>> GetAsync(
        IReadOnlyCollection<Guid> equipmentIds, CancellationToken ct);
}
