namespace MatosKC.Application.EquipmentCategories.Get;

public sealed record GetEquipmentCategoryResult(
    Guid Id,
    string Name,
    string? Description
);
