namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;

public sealed record CancelInspectionRequest(string Reason);

public sealed class CancelInspectionUseCase(InspectionWorkflowContext context)
{
    public Task<InspectionResponse> ExecuteAsync(
        Guid id,
        CancelInspectionRequest request,
        InspectionActor actor,
        CancellationToken ct = default
    )
    {
        InspectionWorkflowContext.RequireOperator(actor);

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
        {
            throw new InspectionOperationException(400, "A cancellation reason of at most 1000 characters is required.");
        }

        return context.UnitOfWork.ExecuteAsync(async token =>
        {
            var (inspection, rental) = await context.GetInspectionAsync(id, actor, token);

            if (inspection.Status == InspectionStatus.Cancelled)
            {
                return InspectionWorkflowContext.Response(inspection, rental);
            }

            if (inspection.Status != InspectionStatus.Draft
                || (inspection.Type == InspectionType.Departure && rental.Status != RentalStatus.Draft)
                || (inspection.Type == InspectionType.Return && rental.Status != RentalStatus.Active))
            {
                throw new InspectionOperationException(409, "Only an unvalidated inspection can be cancelled.");
            }

            var now = context.Time.GetUtcNow();
            inspection.Cancel(actor.AccountId, request.Reason, now);

            if (inspection.Type == InspectionType.Departure)
            {
                rental.Cancel(now);
                await context.Rentals.UpdateAsync(rental, token);
            }

            // No equipment transition: a cancelled return leaves the rental active.
            await context.Inspections.UpdateInspectionAsync(inspection, token);

            return InspectionWorkflowContext.Response(inspection, rental);
        }, ct);
    }
}
