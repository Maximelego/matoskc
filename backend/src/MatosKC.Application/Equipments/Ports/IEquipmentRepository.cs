namespace MatosKC.Application.Equipments.Ports;

using MatosKC.Domain.Equipments;

public interface IEquipmentRepository
{
    Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken);

    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Equipment equipment, CancellationToken cancellationToken);

    Task<Equipment?> RetrieveByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<Equipment>> ListEquipmentsAsync(Guid? categoryId, EquipmentStatus? status, string? search, CancellationToken cancellationToken);

}
