namespace MatosKC.Domain.Entities.Maintenance;

public enum EquipmentDefectStatus
{
    Open,
    Resolved
}

public sealed class EquipmentDefect : BaseEntity
{
    public Guid EquipmentId { get; private set; }
    public Guid InspectionId { get; private set; }
    public Guid InspectionAnswerId { get; private set; }
    public string Description { get; private set; } = null!;
    public string Observation { get; private set; } = null!;
    public DateTimeOffset ReportedAt { get; private set; }
    public EquipmentDefectStatus Status { get; private set; }
    public Guid? ResolvedByMaintenanceId { get; private set; }
    public Guid? ResolvedByAccountId { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private EquipmentDefect() { }

    public EquipmentDefect(Guid id, Guid equipmentId, Guid inspectionId,
        Guid inspectionAnswerId, string description, string observation, DateTimeOffset reportedAt) : base(id)
    {
        if (equipmentId == Guid.Empty || inspectionId == Guid.Empty || inspectionAnswerId == Guid.Empty)
        {
            throw new ArgumentException("Equipment and source inspection identifiers are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(observation);

        if (reportedAt == default)
        {
            throw new ArgumentException("Report date is required.", nameof(reportedAt));
        }

        EquipmentId = equipmentId;
        InspectionId = inspectionId;
        InspectionAnswerId = inspectionAnswerId;
        Description = description;
        Observation = observation;
        ReportedAt = reportedAt.ToUniversalTime();
        Status = EquipmentDefectStatus.Open;
    }

    internal void Resolve(Guid maintenanceId, Guid accountId, DateTimeOffset performedAt)
    {
        if (Status != EquipmentDefectStatus.Open || performedAt < ReportedAt)
        {
            throw new InvalidOperationException("Only an open defect can be resolved, after its report date.");
        }

        if (maintenanceId == Guid.Empty || accountId == Guid.Empty)
        {
            throw new ArgumentException("Maintenance and account identifiers are required.");
        }

        Status = EquipmentDefectStatus.Resolved;
        ResolvedByMaintenanceId = maintenanceId;
        ResolvedByAccountId = accountId;
        ResolvedAt = performedAt.ToUniversalTime();
    }
}
