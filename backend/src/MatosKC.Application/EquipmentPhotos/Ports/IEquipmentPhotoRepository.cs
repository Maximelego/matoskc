namespace MatosKC.Application.EquipmentPhotos.Ports;

using MatosKC.Domain.Equipments;

public interface IEquipmentPhotoRepository
{
    Task AddAsync(EquipmentPhoto photo, CancellationToken cancellationToken);
    Task<EquipmentPhoto?> RetrieveByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EquipmentPhoto>> ListByEquipmentIdAsync(Guid equipmentId, CancellationToken cancellationToken);
    Task UpdateAsync(EquipmentPhoto photo, CancellationToken cancellationToken);
    Task DeleteAsync(EquipmentPhoto photo, CancellationToken cancellationToken);
}
