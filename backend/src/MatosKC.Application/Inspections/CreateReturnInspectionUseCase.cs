namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public sealed class CreateReturnInspectionUseCase(InspectionWorkflowContext context)
{
    public Task<InspectionResponse> ExecuteAsync(
        Guid rentalId,
        CreateReturnRequest request,
        InspectionActor actor,
        CancellationToken cancellationToken = default
    )
    {
        InspectionWorkflowContext.RequireOperator(actor);

        return context.UnitOfWork.ExecuteAsync(async ct =>
        {
            var rental = await context.GetRentalAsync(rentalId, actor, ct);
            var equipment = await context.Equipment.RetrieveByIdAsync(rental.EquipmentId, ct)
                ?? throw new InspectionOperationException(404, "Equipment not found.");

            if (rental.Status != RentalStatus.Active || equipment.Status != EquipmentStatus.Borrowed)
            {
                throw new InspectionOperationException(409, "Rental is not ready for a return inspection.");
            }

            var existing = await context.Inspections.GetForRentalAsync(rental.Id, InspectionType.Return, ct);

            if (existing is not null)
            {
                return InspectionWorkflowContext.Response(existing, rental);
            }

            var template = await context.Templates.GetLatestForCategoryAsync(equipment.CategoryId, ct)
                ?? throw new InspectionOperationException(409, "No inspection template exists for this category.");
            if (template.AgencyId != equipment.AgencyId)
                throw new InspectionOperationException(409, "Template and equipment must belong to the same agency.");
            var inspection = new InspectionEntity(Guid.NewGuid(), rental.Id, template.Id,
                InspectionType.Return, actor.AccountId, request.OperatorFirstName, context.Time.GetUtcNow());

            await OpenInspectionDefects.CaptureAsync(context, inspection, equipment.Id, ct);
            await context.Inspections.AddInspectionAsync(inspection, ct);

            return InspectionWorkflowContext.Response(inspection, rental);
        }, cancellationToken);
    }
}
