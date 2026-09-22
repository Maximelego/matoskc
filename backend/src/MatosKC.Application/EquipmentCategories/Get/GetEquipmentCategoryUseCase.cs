namespace MatosKC.Application.EquipmentCategories.Get;

using MatosKC.Application.EquipmentCategories.Get.Exceptions;
using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Domain.Equipments;

public class GetEquipmentCategoryUseCase
{

    private readonly IEquipmentCategoryRepository EquipmentCategoryRepository;

    public GetEquipmentCategoryUseCase(IEquipmentCategoryRepository equipmentCategoryRepository)
    {
        EquipmentCategoryRepository = equipmentCategoryRepository;
    }

    public async Task<EquipmentCategory> ExecuteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var equipmentCategory = await EquipmentCategoryRepository.GetEquipmentCategoryByIdAsync(categoryId, cancellationToken);
        if (equipmentCategory == null)
        {
            throw new EquipmentCategoryNotFoundException(categoryId);
        }
        return equipmentCategory;
    }

}
