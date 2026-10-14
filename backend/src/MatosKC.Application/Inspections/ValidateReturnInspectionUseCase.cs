namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;

public sealed class ValidateReturnInspectionUseCase(InspectionWorkflowContext context)
{
    public Task<InspectionResponse> ExecuteAsync(
        Guid id,
        InspectionActor actor,
        CancellationToken cancellationToken = default
    )
    {
        InspectionWorkflowContext.RequireOperator(actor);

        return context.UnitOfWork.ExecuteAsync(async ct =>
        {
            var (inspection, rental) = await context.GetInspectionAsync(id, actor, ct);

            if (inspection.Type != InspectionType.Return)
            {
                throw new InspectionOperationException(400, "Wrong inspection type for this operation.");
            }

            // Retrying a successful validation does not overwrite its dates or equipment decision.
            if (inspection.Status == InspectionStatus.Validated)
            {
                return InspectionWorkflowContext.Response(inspection, rental);
            }

            var equipment = await context.Equipment.RetrieveByIdAsync(rental.EquipmentId, ct)
                ?? throw new InspectionOperationException(404, "Equipment not found.");

            if (rental.Status != RentalStatus.Active
                || equipment.Status != EquipmentStatus.Borrowed)
            {
                throw new InspectionOperationException(409, "Rental and equipment are not ready for validation.");
            }

            var template = await context.Templates.GetTemplateByIdAsync(inspection.TemplateId, ct)
                ?? throw new InspectionOperationException(404, "Template not found.");
            var now = context.Time.GetUtcNow();
            inspection.Validate(template, now);
            inspection.AttachReport(await context.Reports.GenerateAsync(inspection, rental, template, ct));
            rental.ConfirmReturn(inspection, now);

            EquipmentStatus nextStatus = inspection.Answers.Any(answer => answer.Result == InspectionResult.NonCompliant)
                ? EquipmentStatus.ToBeDecided
                : EquipmentStatus.Available;
            equipment.ChangeStatus(nextStatus);
            await context.Equipment.UpdateAsync(equipment, ct);

            await context.Inspections.UpdateInspectionAsync(inspection, ct);
            await context.Rentals.UpdateAsync(rental, ct);

            return InspectionWorkflowContext.Response(inspection, rental);
        }, cancellationToken);
    }
}
