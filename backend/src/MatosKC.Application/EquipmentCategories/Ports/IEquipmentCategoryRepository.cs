namespace MatosKC.Application.EquipmentCategories.Ports;

using MatosKC.Domain.Equipments;

public interface IEquipmentCategoryRepository
{

    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string Name, CancellationToken cancellationToken);

    Task AddAsync(EquipmentCategory category, CancellationToken cancellationToken);

    Task<EquipmentCategory?> GetEquipmentCategoryByIdAsync(Guid id, CancellationToken cancellationToken);

}
