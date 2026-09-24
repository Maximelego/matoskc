namespace MatosKC.Application.EquipmentCategories.Ports;

using MatosKC.Domain.Equipments;

public interface IEquipmentCategoryRepository
{

    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string Name, CancellationToken cancellationToken);

    Task AddAsync(EquipmentCategory category, CancellationToken cancellationToken);

    Task<List<EquipmentCategory>> ListEquipmentCategoriesAsync(CancellationToken cancellationToken);

    Task<EquipmentCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

}
