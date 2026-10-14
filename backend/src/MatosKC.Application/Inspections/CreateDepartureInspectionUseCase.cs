namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public sealed class CreateDepartureInspectionUseCase(InspectionWorkflowContext context)
{
    public Task<InspectionResponse> ExecuteAsync(
        CreateDepartureRequest request,
        InspectionActor actor,
        CancellationToken cancellationToken = default
    )
    {
        InspectionWorkflowContext.RequireOperator(actor);
        ArgumentNullException.ThrowIfNull(request.Client);

        return context.UnitOfWork.ExecuteAsync(async ct =>
        {
            var equipment = await context.Equipment.RetrieveByIdAsync(request.EquipmentId, ct)
                ?? throw new InspectionOperationException(404, "Equipment not found.");

            if (equipment.Status != EquipmentStatus.Available)
            {
                throw new InspectionOperationException(409, "Equipment is not available for departure.");
            }

            if (actor.Role == MatosKC.Domain.Entities.Accounts.Role.Agency
                && actor.AgencyCode != request.Client.AgencyCode)
            {
                throw new InspectionOperationException(403, "Cannot create a rental for another agency.");
            }

            var agency = await context.Agencies.GetByCodeAsync(request.Client.AgencyCode, ct)
                ?? throw new InspectionOperationException(404, "Agency not found.");

            if (actor.Role == MatosKC.Domain.Entities.Accounts.Role.Agency && actor.AgencyId != agency.Id)
            {
                throw new InspectionOperationException(403, "Cannot create a rental for another agency.");
            }

            if (await context.Rentals.GetOpenForEquipmentAsync(equipment.Id, ct) is not null)
            {
                throw new InspectionOperationException(409, "An open rental already exists for this equipment.");
            }

            var template = await context.Templates.GetLatestForCategoryAsync(equipment.CategoryId, ct)
                ?? throw new InspectionOperationException(409, "No inspection template exists for this category.");
            var now = context.Time.GetUtcNow();
            var client = new Client(Guid.NewGuid(), request.Client.DisplayName, request.Client.Email,
                request.Client.Phone, new CommandNumber(request.Client.AgencyCode, request.Client.CommandValue));
            var rental = new Rental(Guid.NewGuid(), client.Id, equipment.Id, now, agency.Id);
            var inspection = new InspectionEntity(Guid.NewGuid(), rental.Id, template.Id,
                InspectionType.Departure, actor.AccountId, request.OperatorFirstName, now);

            await context.Clients.AddClientAsync(client, ct);
            await context.Rentals.AddAsync(rental, ct);
            await context.Inspections.AddInspectionAsync(inspection, ct);

            return InspectionWorkflowContext.Response(inspection, rental);
        }, cancellationToken);
    }
}
