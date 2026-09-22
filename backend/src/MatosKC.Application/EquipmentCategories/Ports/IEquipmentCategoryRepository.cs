namespace MatosKC.Application.Equipment.Ports;

public interface IEquipmentCategoryRepository
{

    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken);

}
