namespace MatosKC.Application.Equipments;

using MatosKC.Application.Inspections;
using MatosKC.Application.Persistence;
using MatosKC.Application.Equipments.Ports;

public sealed class GetEquipmentHistoryUseCase(IAuditRepository audit, IEquipmentRepository equipments)
{
    public async Task<IReadOnlyList<AuditEntry>> ExecuteAsync(Guid id, int offset, int limit,
        InspectionActor actor, CancellationToken ct = default)
    {
        InspectionWorkflowContext.RequireAdministrator(actor);

        if (offset < 0 || limit is < 1 or > 100)
        {
            throw new InspectionOperationException(400, "Invalid pagination (limit 1 to 100).");
        }

        if (!await equipments.ExistsByIdAsync(id, ct))
        {
            throw new InspectionOperationException(404, "Equipment not found.");
        }

        return await audit.ListAsync("Equipment", id, offset, limit, ct);
    }
}
