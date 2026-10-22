namespace MatosKC.Application.Equipments;

using MatosKC.Application.Inspections;
using MatosKC.Domain.Equipments;

public sealed record UpdateEquipmentRequest(string Name, Guid EquipmentCategoryId);
public sealed record UpdateCategoryRequest(string Name, string? Description);

public sealed class ManageEquipmentUseCase(InspectionWorkflowContext context)
{
    public Task<Equipment> UpdateAsync(Guid id, UpdateEquipmentRequest request,
        InspectionActor actor, CancellationToken ct = default)
    {
        InspectionWorkflowContext.RequireAdministrator(actor);

        return context.UnitOfWork.ExecuteAsync(async token =>
        {
            var equipment = await context.Equipment.RetrieveByIdAsync(id, token)
                ?? throw new InspectionOperationException(404, "Equipment not found.");

            var targetCategory = await context.Categories.GetByIdAsync(request.EquipmentCategoryId, token);
            if (targetCategory is null)
            {
                throw new InspectionOperationException(404, "Category not found.");
            }

            if (equipment.AgencyId != targetCategory.AgencyId)
                throw new InspectionOperationException(409, "Equipment and category must belong to the same agency.");

            if (equipment.CategoryId != request.EquipmentCategoryId
                && await context.Rentals.GetOpenForEquipmentAsync(id, token) is not null)
            {
                throw new InspectionOperationException(409, "Category cannot change during an open rental.");
            }

            equipment.UpdateDetails(request.Name, request.EquipmentCategoryId);
            await context.Equipment.UpdateAsync(equipment, token);

            return equipment;
        }, ct);
    }

    public Task<EquipmentCategory> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request,
        InspectionActor actor, CancellationToken ct = default)
    {
        InspectionWorkflowContext.RequireAdministrator(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        return context.UnitOfWork.ExecuteAsync(async token =>
        {
            var category = await context.Categories.GetByIdAsync(id, token)
                ?? throw new InspectionOperationException(404, "Category not found.");

            if (category.Name != request.Name.Trim()
                && await context.Categories.ExistsByNameAsync(request.Name.Trim(), token, category.AgencyId == Guid.Empty ? null : category.AgencyId))
            {
                throw new InspectionOperationException(409, "Category name already exists.");
            }

            category.UpdateDetails(request.Name, request.Description);
            await context.Categories.UpdateAsync(category, token);

            return category;
        }, ct);
    }
}
