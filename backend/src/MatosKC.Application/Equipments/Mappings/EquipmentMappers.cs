namespace MatosKC.Application.Equipments.Mappings;

using MatosKC.Application.Equipments.Create;
using MatosKC.Domain.Equipments;

public static class EquipmentMapper
{
    public static Equipment ToDomain(
        this CreateEquipmentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Equipment(
            dto.Name,
            dto.CategoryId,
            dto.SerialNumber
        );
    }
}
