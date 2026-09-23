namespace MatosKC.Application.EquipmentCategories.Get;

using MatosKC.Application.EquipmentCategories.Get.Exceptions;
using MatosKC.Application.EquipmentCategories.Mappings;
using MatosKC.Application.EquipmentCategories.Ports;

public class GetEquipmentCategoryUseCase
{

    private readonly IEquipmentCategoryRepository EquipmentCategoryRepository;

    public GetEquipmentCategoryUseCase(IEquipmentCategoryRepository equipmentCategoryRepository)
    {
        EquipmentCategoryRepository = equipmentCategoryRepository;
    }

    public async Task<GetEquipmentCategoryResult> ExecuteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var equipmentCategory = await EquipmentCategoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (equipmentCategory == null)
        {
            throw new EquipmentCategoryNotFoundException(categoryId);
        }
        return equipmentCategory.ToResult();
    }

}
