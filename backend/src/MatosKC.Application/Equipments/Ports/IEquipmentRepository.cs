namespace MatosKC.Application.Equipment.Ports;

using MatosKC.Domain.Equipments;

public interface IEquipmentRepository
{
    Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken);

    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Equipment equipment, CancellationToken cancellationToken);

}
