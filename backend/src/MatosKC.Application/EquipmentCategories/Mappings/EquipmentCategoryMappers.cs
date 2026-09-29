namespace MatosKC.Application.EquipmentCategories.Mappings;

using MatosKC.Application.EquipmentCategories.Create;
using MatosKC.Application.EquipmentCategories.Get;
using MatosKC.Domain.Equipments;

public static class EquipmentCategoryMapper
{
    public static EquipmentCategory ToDomain(
        this CreateEquipmentCategoryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new EquipmentCategory(
            dto.Name,
            dto.Description
        );
    }

    public static GetEquipmentCategoryResult ToResult(
        this EquipmentCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new GetEquipmentCategoryResult(
            category.Id,
            category.Name,
            category.Description
        );
    }
}
