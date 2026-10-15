namespace MatosKC.Application.Equipments.List;

using MatosKC.Application.Equipments.Mappings;
using MatosKC.Application.Equipments.Ports;

public class ListEquipmentUseCase
{
    private readonly IEquipmentRepository _equipmentRepository;

    private readonly MatosKC.Application.EquipmentPhotos.Ports.IEquipmentThumbnailRepository? Thumbnails;

    public ListEquipmentUseCase(IEquipmentRepository equipmentRepository,
        MatosKC.Application.EquipmentPhotos.Ports.IEquipmentThumbnailRepository? thumbnails = null)
    {
        _equipmentRepository = equipmentRepository;
        Thumbnails = thumbnails;
    }

    public async Task<ListEquipmentResult> ExecuteAsync(ListEquipmentsQuery query, CancellationToken cancellationToken)
    {
        var equipments = await _equipmentRepository.ListEquipmentsAsync(query.CategoryId, query.Status, query.Search, cancellationToken);

        var thumbnails = Thumbnails is null ? null
            : await Thumbnails.GetAsync(equipments.Select(equipment => equipment.Id).ToArray(), cancellationToken);

        return new ListEquipmentResult(equipments.ConvertAll(equipment =>
        {
            var dto = equipment.ToDto();

            if (thumbnails is not null && thumbnails.TryGetValue(equipment.Id, out var photo))
            {
                dto = dto with
                {
                    PrimaryPhotoId = photo.PhotoId,
                    ThumbnailUrl = $"/equipments/{equipment.Id}/photos/{photo.PhotoId}?v={photo.UpdatedAtUtc.UtcTicks}"
                };
            }

            return dto;
        }));
    }
}
