namespace MatosKC.Domain.Entities.Rental;

using MatosKC.Domain.Entities.Inspection;

public class Rental : BaseEntity
{
    public Guid ClientId { get; private set; }

    public Guid? AgencyId { get; private set; }

    public Guid EquipmentId { get; private set; }

    public RentalStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? DepartureInspectionId { get; private set; }

    public Guid? ReturnInspectionId { get; private set; }

    public DateTimeOffset? DepartedAt { get; private set; }

    public DateTimeOffset? ReturnedAt { get; private set; }

    public Rental(
        Guid id,
        Guid clientId,
        Guid equipmentId,
        DateTimeOffset createdAt,
        Guid? agencyId = null
    ) : base(id)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Client ID cannot be empty.", nameof(clientId));
        }

        if (equipmentId == Guid.Empty)
        {
            throw new ArgumentException("Equipment ID cannot be empty.", nameof(equipmentId));
        }

        if (agencyId == Guid.Empty)
        {
            throw new ArgumentException("Agency ID cannot be empty.", nameof(agencyId));
        }

        AgencyId = agencyId;
        ClientId = clientId;
        EquipmentId = equipmentId;
        CreatedAt = createdAt;
        Status = RentalStatus.Draft;
    }

    public void ConfirmDeparture(
        Inspection departureInspection,
        DateTimeOffset departedAt
    )
    {
        ArgumentNullException.ThrowIfNull(departureInspection);

        if (Status != RentalStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft rental can confirm departure.");
        }

        EnsureInspection(departureInspection, InspectionType.Departure);

        if (departedAt < CreatedAt)
        {
            throw new ArgumentException(
                "Departure date cannot be earlier than the rental creation date.",
                nameof(departedAt)
            );
        }

        DepartureInspectionId = departureInspection.Id;
        DepartedAt = departedAt;
        Status = RentalStatus.Active;
    }

    public void ConfirmReturn(
        Inspection returnInspection,
        DateTimeOffset returnedAt
    )
    {
        ArgumentNullException.ThrowIfNull(returnInspection);

        if (Status != RentalStatus.Active || !DepartedAt.HasValue)
        {
            throw new InvalidOperationException("Only an active rental can confirm return.");
        }

        EnsureInspection(returnInspection, InspectionType.Return);

        if (returnedAt < DepartedAt.Value)
        {
            throw new ArgumentException(
                "Return date cannot be earlier than the departure date.",
                nameof(returnedAt)
            );
        }

        // Departure and return may use different immutable templates.
        ReturnInspectionId = returnInspection.Id;
        ReturnedAt = returnedAt;
        Status = RentalStatus.Completed;
    }

    private void EnsureInspection(
        Inspection inspection,
        InspectionType expectedType
    )
    {
        if (inspection.RentalId != Id)
        {
            throw new ArgumentException(
                "Inspection does not belong to this rental.",
                nameof(inspection)
            );
        }

        if (inspection.Type != expectedType)
        {
            throw new ArgumentException(
                "Inspection type does not match the rental operation.",
                nameof(inspection)
            );
        }

        if (inspection.Status != InspectionStatus.Validated)
        {
            throw new ArgumentException("Inspection must be validated.", nameof(inspection));
        }
    }
}
