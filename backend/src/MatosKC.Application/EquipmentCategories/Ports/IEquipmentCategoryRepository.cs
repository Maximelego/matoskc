namespace MatosKC.Application.Equipment.Ports;

using MatosKC.Domain.Equipments;

public interface IEquipmentCategoryRepository
{

    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken);

}
