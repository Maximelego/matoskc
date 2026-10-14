namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;

public sealed class QueryInspectionsUseCase(InspectionWorkflowContext context)
{
    public async Task<InspectionResponse> GetAsync(Guid id, InspectionActor actor, CancellationToken ct = default)
    {
        var (inspection, rental) = await context.GetInspectionAsync(id, actor, ct);

        return InspectionWorkflowContext.Response(inspection, rental);
    }

    public async Task<IReadOnlyList<InspectionResponse>> ListAsync(
        InspectionActor actor,
        Guid? rentalId,
        InspectionType? type,
        InspectionStatus? status,
        int offset = 0,
        int limit = 50,
        CancellationToken ct = default
    )
    {
        InspectionWorkflowContext.RequireReader(actor);

        if (offset < 0 || limit is < 1 or > 100
            || (type.HasValue && !Enum.IsDefined(type.Value))
            || (status.HasValue && !Enum.IsDefined(status.Value)))
        {
            throw new InspectionOperationException(400, "Invalid pagination or inspection filter.");
        }

        if (actor.Role == Role.Agency && !rentalId.HasValue)
        {
            throw new InspectionOperationException(403, "An agency must query a specific rental.");
        }

        if (rentalId.HasValue)
        {
            await context.GetRentalAsync(rentalId.Value, actor, ct);
        }

        var inspections = await context.Inspections.ListAsync(rentalId, type, status, offset, limit, ct);
        var results = new List<InspectionResponse>();

        foreach (var inspection in inspections)
        {
            var rental = await context.GetRentalAsync(inspection.RentalId, actor, ct);
            results.Add(InspectionWorkflowContext.Response(inspection, rental));
        }

        return results;
    }

    public async Task<RentalResponse> GetRentalAsync(Guid id, InspectionActor actor, CancellationToken ct = default)
    {
        var rental = await context.GetRentalAsync(id, actor, ct);

        return await context.RentalResponseAsync(rental, ct);
    }

    public async Task<RentalResponse?> GetOpenRentalAsync(
        Guid equipmentId,
        InspectionActor actor,
        CancellationToken ct = default
    )
    {
        InspectionWorkflowContext.RequireReader(actor);

        if (await context.Equipment.RetrieveByIdAsync(equipmentId, ct) is null)
        {
            throw new InspectionOperationException(404, "Equipment not found.");
        }

        var rental = await context.Rentals.GetOpenForEquipmentAsync(equipmentId, ct);

        if (rental is null)
        {
            return null;
        }

        await context.GetRentalAsync(rental.Id, actor, ct);

        return await context.RentalResponseAsync(rental, ct);
    }

    public async Task<InspectionResponse?> GetDepartureAsync(Guid rentalId, InspectionActor actor, CancellationToken ct = default)
    {
        var rental = await context.GetRentalAsync(rentalId, actor, ct);
        var inspection = await context.Inspections.GetForRentalAsync(rental.Id, InspectionType.Departure, ct);

        return inspection is null ? null : InspectionWorkflowContext.Response(inspection, rental);
    }
}
