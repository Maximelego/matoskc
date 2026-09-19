namespace MatosKC.Application.Equipments.Create;

public sealed record CreateEquipmentDto(
    string Name,
    Guid CategoryId,
    string SerialNumber
);