namespace MatosKC.Domain.Entities.Rental;

using MatosKC.Domain.Entities.Inspection;

public class Rental: BaseEntity
{
    public Guid ClientId { get; private set; }
    public Guid EquipmentId { get; private set; }
    public RentalStatus? Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid DepartureInspectionId { get; private set; }
    public Guid ReturnInspectionId { get; private set; }
    public DateTimeOffset? DepartedAt { get; private set; }
    public DateTimeOffset? ReturnedAt { get; private set; }

    public Rental(
        Guid id,
        Guid clientId,
        Guid equipmentId,
        RentalStatus? status,
        DateTimeOffset createdAt,
        Guid departureInspectionId,
        Guid returnInspectionId
    ) : base(id)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("Client ID cannot be empty.", nameof(clientId));

        if (equipmentId == Guid.Empty)
            throw new ArgumentException("Equipment ID cannot be empty.", nameof(equipmentId));

        if (status != null && !Enum.IsDefined(typeof(RentalStatus), status))
            throw new ArgumentException("Invalid rental status.", nameof(status));

        if (departureInspectionId == Guid.Empty)
            throw new ArgumentException("Departure inspection ID cannot be empty.", nameof(departureInspectionId));

        if (returnInspectionId == Guid.Empty)
            throw new ArgumentException("Return inspection ID cannot be empty.", nameof(returnInspectionId));

        ClientId = clientId;
        EquipmentId = equipmentId;
        Status = status;
        CreatedAt = createdAt;
        DepartureInspectionId = departureInspectionId;
        ReturnInspectionId = returnInspectionId;
    }

    public void ConfirmDeparture(Inspection departureInspection, InspectionTemplate template, DateTimeOffset departedAt)
    {
        if (departureInspection.Id != DepartureInspectionId)
            throw new ArgumentException("Departure inspection does not match this rental.", nameof(departureInspection));

        if (departureInspection.Type != InspectionType.Departure)
            throw new ArgumentException("Inspection is not a departure inspection.", nameof(departureInspection));

        if (departureInspection.TemplateId != template.Id)
            throw new ArgumentException("Template does not match the departure inspection.", nameof(template));

        departureInspection.Validate(template, DateTimeOffset.UtcNow);
        DepartedAt = departedAt;

    }

    public void ConfirmReturn(Inspection returnInspection, InspectionTemplate template, DateTimeOffset returnedAt)
    {
        if (Status != RentalStatus.Active)
            throw new InvalidOperationException("Rental is not active.");

        if (returnInspection.Id != ReturnInspectionId)
            throw new ArgumentException("Return inspection does not match this rental.", nameof(returnInspection));

        if (returnInspection.Type != InspectionType.Return)
            throw new ArgumentException("Inspection is not a return inspection.", nameof(returnInspection));

        returnInspection.Validate(template, DateTimeOffset.UtcNow);

        ReturnedAt = returnedAt;
        Status = RentalStatus.Completed;
    }
}
