using MatosKC.Application.EquipmentCategories.Mappings;
using MatosKC.Application.EquipmentCategories.Ports;

namespace MatosKC.Application.EquipmentCategories.List;

public class ListEquipmentCategoryUseCase
{
    private readonly IEquipmentCategoryRepository _equipmentCategoryRepository;

    public ListEquipmentCategoryUseCase(IEquipmentCategoryRepository equipmentCategoryRepository)
    {
        _equipmentCategoryRepository = equipmentCategoryRepository;
    }

    public async Task<ListEquipmentCategoryResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var equipmentCategories = await _equipmentCategoryRepository.ListEquipmentCategoriesAsync(cancellationToken);

        return new ListEquipmentCategoryResult(
            equipmentCategories.ConvertAll(equipmentCategory => equipmentCategory.ToResult())
        );
    }
}
