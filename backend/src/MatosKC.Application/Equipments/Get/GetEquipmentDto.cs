
namespace MatosKC.Application.Equipments.Get;

public record GetEquipmentDto(
    Guid Id,
    string Name,
    string SerialNumber,
    Guid EquipmentCategoryId
);
