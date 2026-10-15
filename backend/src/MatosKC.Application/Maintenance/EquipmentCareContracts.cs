namespace MatosKC.Application.Maintenance;

using MatosKC.Domain.Entities.Maintenance;

// A client-generated identifier makes a retried creation idempotent.
public sealed record RecordMaintenanceRequest(Guid Id, DateTimeOffset PerformedAt,
    string PerformedBy, string WorkDescription, IReadOnlyList<Guid> ResolvedDefectIds);

public sealed record DefectOccurrenceResponse(Guid Id, Guid InspectionId, Guid InspectionAnswerId,
    string Description, string Observation, DateTimeOffset ObservedAt, IReadOnlyList<Guid> PhotoIds);

public sealed record EquipmentDefectResponse(Guid Id, Guid EquipmentId,
    Guid InspectionId, Guid InspectionAnswerId, string Description, string Observation,
    DateTimeOffset ReportedAt, EquipmentDefectStatus Status, Guid? ResolvedByMaintenanceId,
    Guid? ResolvedByAccountId, DateTimeOffset? ResolvedAt, IReadOnlyList<DefectOccurrenceResponse> Occurrences);

public sealed record MaintenanceResponse(Guid Id, Guid EquipmentId, DateTimeOffset PerformedAt,
    string PerformedBy, string WorkDescription, Guid RecordedByAccountId, DateTimeOffset RecordedAt,
    IReadOnlyList<Guid> ResolvedDefectIds)
{
    public static MaintenanceResponse From(EquipmentMaintenance maintenance) => new(
        maintenance.Id, maintenance.EquipmentId, maintenance.PerformedAt, maintenance.PerformedBy,
        maintenance.WorkDescription, maintenance.RecordedByAccountId, maintenance.RecordedAt,
        maintenance.ResolvedDefectIds.ToArray());
}
