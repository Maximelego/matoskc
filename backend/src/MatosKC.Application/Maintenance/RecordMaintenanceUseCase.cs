namespace MatosKC.Application.Maintenance;

using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Maintenance;

public sealed class RecordMaintenanceUseCase(InspectionWorkflowContext context)
{
    public Task<MaintenanceResponse> ExecuteAsync(Guid equipmentId, RecordMaintenanceRequest request,
        InspectionActor actor, CancellationToken ct = default)
    {
        InspectionWorkflowContext.RequireAdministrator(actor);
        ArgumentNullException.ThrowIfNull(request.ResolvedDefectIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PerformedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkDescription);

        if (request.Id == Guid.Empty || request.ResolvedDefectIds.Contains(Guid.Empty)
            || request.ResolvedDefectIds.Distinct().Count() != request.ResolvedDefectIds.Count
            || request.ResolvedDefectIds.Count > 100)
        {
            throw new InspectionOperationException(400, "Provide an intervention identifier and up to 100 distinct defect identifiers.");
        }

        // PostgreSQL timestamps have microsecond precision. Normalize retry comparisons too.
        var performedAt = request.PerformedAt.ToUniversalTime();
        performedAt = performedAt.AddTicks(-(performedAt.Ticks % 10));

        return context.UnitOfWork.ExecuteAsync(async token =>
        {
            var previous = await context.Care.GetMaintenanceAsync(request.Id, token);

            if (previous is not null)
            {
                if (previous.EquipmentId != equipmentId || previous.RecordedByAccountId != actor.AccountId
                    || previous.PerformedAt != performedAt || previous.PerformedBy != request.PerformedBy.Trim()
                    || previous.WorkDescription != request.WorkDescription.Trim()
                    || !previous.ResolvedDefectIds.Order().SequenceEqual(request.ResolvedDefectIds.Order()))
                {
                    throw new InspectionOperationException(409, "Intervention identifier already used with different data.");
                }

                return MaintenanceResponse.From(previous);
            }

            if (!await context.Equipment.ExistsByIdAsync(equipmentId, token))
            {
                throw new InspectionOperationException(404, "Equipment not found.");
            }

            var defects = new List<EquipmentDefect>();

            foreach (Guid id in request.ResolvedDefectIds)
            {
                defects.Add(await RecordInspectionDefects.RequireOpenAsync(context.Care, id, equipmentId, token));
            }

            var occurrences = await context.Care.GetOccurrencesAsync(request.ResolvedDefectIds.ToArray(), token);

            if (occurrences.Any(occurrence => occurrence.ObservedAt > performedAt))
            {
                throw new InspectionOperationException(409,
                    "An intervention cannot resolve a defect observed after the intervention date.");
            }

            var maintenance = EquipmentMaintenance.Record(request.Id, equipmentId, performedAt,
                request.PerformedBy, request.WorkDescription, actor.AccountId, context.Time.GetUtcNow(), defects);
            await context.Care.AddMaintenanceAsync(maintenance, token);

            // Resolving a defect never decides the equipment's availability.
            return MaintenanceResponse.From(maintenance);
        }, ct);
    }
}
