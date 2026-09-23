using MatosKC.Application.Equipments.Get;

namespace MatosKC.Application.Equipments.List;

public sealed record ListEquipmentResult(
    List<GetEquipmentDto> Equipments
);
