namespace MatosKC.Application.Equipments.Mappings;

using MatosKC.Application.Equipments.Create;
using MatosKC.Application.Equipments.Get;
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

    public static GetEquipmentDto ToDto(
        this Equipment equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        return new GetEquipmentDto(
            equipment.Id,
            equipment.Name,
            equipment.SerialNumber,
            equipment.CategoryId
        );
    }
}
