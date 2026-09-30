
using MatosKC.Domain.Equipments;

namespace MatosKC.Application.Equipments.Get;

public record GetEquipmentDto(
    Guid Id,
    string Name,
    string SerialNumber,
    EquipmentStatus Status,
    Guid EquipmentCategoryId
);
