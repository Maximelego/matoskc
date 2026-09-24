using MatosKC.Application.EquipmentCategories.Get;

namespace MatosKC.Application.EquipmentCategories.List;

public sealed record ListEquipmentCategoryResult(
    List<GetEquipmentCategoryResult> EquipmentCategories
);
