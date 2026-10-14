namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Equipments;

public sealed class ChangeEquipmentStatusUseCase(InspectionWorkflowContext context)
{
    public Task<Equipment> ExecuteAsync(
        Guid equipmentId,
        EquipmentStatus status,
        InspectionActor actor,
        CancellationToken ct = default
    )
    {
        InspectionWorkflowContext.RequireAdministrator(actor);

        return context.UnitOfWork.ExecuteAsync(async token =>
        {
            var equipment = await context.Equipment.RetrieveByIdAsync(equipmentId, token)
                ?? throw new InspectionOperationException(404, "Equipment not found.");

            if (await context.Rentals.GetOpenForEquipmentAsync(equipment.Id, token) is not null)
            {
                throw new InspectionOperationException(409, "Equipment has an open rental.");
            }

            // Borrowing is exclusively driven by validation of a departure inspection.
            if (status == EquipmentStatus.Borrowed || equipment.Status == EquipmentStatus.Borrowed)
            {
                throw new InspectionOperationException(409, "Borrowed status is managed by inspections.");
            }

            equipment.ChangeStatus(status);
            await context.Equipment.UpdateAsync(equipment, token);

            return equipment;
        }, ct);
    }
}
