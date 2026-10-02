namespace MatosKC.Application.EquipmentPhotos.List;

using MatosKC.Application.Equipments.Get.Exceptions;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Domain.Equipments;

public sealed class ListEquipmentPhotosUseCase
{
    private readonly IEquipmentRepository EquipmentRepository;
    private readonly IEquipmentPhotoRepository PhotoRepository;

    public ListEquipmentPhotosUseCase(
        IEquipmentRepository equipmentRepository,
        IEquipmentPhotoRepository photoRepository
    )
    {
        EquipmentRepository = equipmentRepository;
        PhotoRepository = photoRepository;
    }

    public async Task<IReadOnlyList<EquipmentPhotoResult>> ExecuteAsync(
        Guid equipmentId,
        CancellationToken cancellationToken = default
    )
    {
        if (!await EquipmentRepository.ExistsByIdAsync(equipmentId, cancellationToken))
        {
            throw new EquipmentNotFoundException(equipmentId);
        }

        IReadOnlyList<EquipmentPhoto> photos =
            await PhotoRepository.ListByEquipmentIdAsync(equipmentId, cancellationToken);

        return photos.Select(EquipmentPhotoResult.FromDomain).ToArray();
    }
}
