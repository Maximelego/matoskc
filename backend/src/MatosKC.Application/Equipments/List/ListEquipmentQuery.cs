using MatosKC.Domain.Equipments;

namespace MatosKC.Application.Equipments.List;

public sealed record ListEquipmentsQuery(
    Guid? CategoryId,
    EquipmentStatus? Status,
    string? Search
);
