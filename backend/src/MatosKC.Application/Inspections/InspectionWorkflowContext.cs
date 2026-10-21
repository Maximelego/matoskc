namespace MatosKC.Application.Inspections;

using MatosKC.Application.Clients.Ports;
using MatosKC.Application.Rentals.Ports;
using MatosKC.Application.Inspections.Ports;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Application.Persistence;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Entities.Inspection;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public sealed class InspectionWorkflowContext(
    IClientRepository clients,
    IRentalRepository rentals,
    IInspectionsRepository inspections,
    IInspectionTemplateRepository templates,
    IInspectionMediaRepository media,
    IEquipmentRepository equipment,
    IEquipmentCategoryRepository categories,
    IAgencyRepository agencies,
    IObjectStorage storage,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    IInspectionReportGenerator reports,
    IInspectionSignatureValidator signatureValidator,
    IInspectionTemplatePhotoRepository templatePhotos,
    IImageContentValidator imageValidator,
    MatosKC.Application.Maintenance.IEquipmentCareRepository care
)
{
    public MatosKC.Application.Maintenance.IEquipmentCareRepository Care { get; } = care;

    public IClientRepository Clients { get; } = clients;
    public IRentalRepository Rentals { get; } = rentals;
    public IInspectionsRepository Inspections { get; } = inspections;
    public IInspectionTemplateRepository Templates { get; } = templates;
    public IInspectionMediaRepository Media { get; } = media;
    public IEquipmentRepository Equipment { get; } = equipment;
    public IEquipmentCategoryRepository Categories { get; } = categories;
    public IAgencyRepository Agencies { get; } = agencies;
    public IObjectStorage Storage { get; } = storage;
    public IUnitOfWork UnitOfWork { get; } = unitOfWork;
    public TimeProvider Time { get; } = time;
    public IInspectionReportGenerator Reports { get; } = reports;
    public IInspectionSignatureValidator SignatureValidator { get; } = signatureValidator;
    public IInspectionTemplatePhotoRepository TemplatePhotos { get; } = templatePhotos;
    public IImageContentValidator ImageValidator { get; } = imageValidator;

    public static void RequireReader(InspectionActor actor)
    {
        if (actor.AccountId == Guid.Empty || !Enum.IsDefined(actor.Role))
        {
            throw new InspectionOperationException(403, "A valid account is required.");
        }
    }

    public static void RequireOperator(InspectionActor actor)
    {
        if (actor.AccountId == Guid.Empty || actor.Role is not (Role.Agency or Role.SuperAdmin)
            || (actor.Role == Role.Agency && (!actor.AgencyId.HasValue || actor.AgencyId == Guid.Empty)))
        {
            throw new InspectionOperationException(403, "This account cannot perform inspections.");
        }
    }

    public static void RequireAdministrator(InspectionActor actor)
    {
        if (actor.AccountId == Guid.Empty || actor.Role is not (Role.Admin or Role.SuperAdmin))
        {
            throw new InspectionOperationException(403, "Administrator access is required.");
        }
    }

    public async Task<Rental> GetRentalAsync(Guid id, InspectionActor actor, CancellationToken ct)
    {
        RequireReader(actor);

        var rental = await Rentals.GetByIdAsync(id, ct)
            ?? throw new InspectionOperationException(404, "Rental not found.");

        var client = await Clients.GetByIdAsync(rental.ClientId, ct)
            ?? throw new InspectionOperationException(404, "Rental client not found.");

        if (actor.Role == Role.Agency
            && (!rental.AgencyId.HasValue || actor.AgencyId != rental.AgencyId))
        {
            throw new InspectionOperationException(403, "Rental belongs to another agency.");
        }

        return rental;
    }

    public async Task<(InspectionEntity Inspection, Rental Rental)> GetInspectionAsync(
        Guid id,
        InspectionActor actor,
        CancellationToken ct
    )
    {
        var inspection = await Inspections.GetInspectionByIdAsync(id, ct)
            ?? throw new InspectionOperationException(404, "Inspection not found.");
        var rental = await GetRentalAsync(inspection.RentalId, actor, ct);

        return (inspection, rental);
    }

    public static InspectionResponse Response(InspectionEntity inspection, Rental rental) => new(
        inspection.Id,
        inspection.RentalId,
        rental.EquipmentId,
        inspection.TemplateId,
        inspection.Type,
        inspection.Status,
        inspection.CreatedByAccountId,
        inspection.OperatorFirstName,
        inspection.StartedAt,
        inspection.ValidatedAt,
        inspection.Answers.Select(answer => new InspectionAnswerResponse(
            answer.Id, answer.TemplatePointId, answer.Result, answer.Comment, answer.PhotosIds.ToArray(), answer.ExistingDefectId
        )).ToArray(),
        inspection.SignerName,
        inspection.SignedAt,
        inspection.AcceptanceText,
        inspection.HasReport,
        inspection.CancelledAt,
        inspection.CancelledByAccountId,
        inspection.CancellationReason,
        inspection.KnownDefects.Select(value => new DefectReviewResponse(value.Id, value.DefectId,
            value.Description, value.HistoricalObservation, value.ObservedAt, value.HistoricalPhotoIds.ToArray(),
            value.Result, value.Observation, value.PhotoIds.ToArray())).ToArray(),
        inspection.Findings.Select(value => new FindingResponse(value.Id, value.TemplatePointId,
            value.Observation, value.PhotoIds.ToArray())).ToArray()
    );

    public async Task<RentalResponse> RentalResponseAsync(Rental rental, CancellationToken ct)
    {
        var client = await Clients.GetByIdAsync(rental.ClientId, ct)
            ?? throw new InspectionOperationException(404, "Client not found.");

        return new RentalResponse(
            rental.Id, rental.ClientId, rental.EquipmentId, rental.Status, rental.CreatedAt,
            rental.DepartureInspectionId, rental.ReturnInspectionId, rental.DepartedAt, rental.ReturnedAt,
            new ClientInput(client.DisplayName, client.Email, client.Phone,
                client.CommandNumber.AgencyCode, client.CommandNumber.Value),
            rental.CancelledAt
        );
    }
}
