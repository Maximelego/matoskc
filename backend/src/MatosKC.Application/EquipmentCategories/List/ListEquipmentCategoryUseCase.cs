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

    public async Task<ListEquipmentCategoryResult> ExecuteAsync(ListEquipmentCategoryQuery query, CancellationToken cancellationToken)
    {
        var equipmentCategories = await _equipmentCategoryRepository.ListEquipmentCategoriesAsync(query, cancellationToken);

        return new ListEquipmentCategoryResult(
            equipmentCategories.Select(equipmentCategory => equipmentCategory.ToResult())
                               .ToList()
        );
    }
}
