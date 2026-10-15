namespace MatosKC.Application.Maintenance;

using MatosKC.Domain.Entities.Maintenance;

public sealed record CarePage<T>(IReadOnlyList<T> Items, int Total, int Offset, int Limit);

public interface IEquipmentCareRepository
{
    Task<EquipmentDefect?> GetDefectAsync(Guid id, CancellationToken ct);
    Task AddDefectAsync(EquipmentDefect defect, CancellationToken ct);
    Task AddOccurrenceAsync(DefectOccurrence occurrence, CancellationToken ct);
    Task<IReadOnlyList<DefectOccurrence>> GetOccurrencesAsync(IReadOnlyCollection<Guid> defectIds, CancellationToken ct);
    Task<CarePage<EquipmentDefect>> ListDefectsAsync(Guid equipmentId, EquipmentDefectStatus? status,
        int offset, int limit, CancellationToken ct);
    Task<EquipmentMaintenance?> GetMaintenanceAsync(Guid id, CancellationToken ct);
    Task AddMaintenanceAsync(EquipmentMaintenance maintenance, CancellationToken ct);
    Task<CarePage<EquipmentMaintenance>> ListMaintenancesAsync(Guid equipmentId,
        int offset, int limit, CancellationToken ct);
}
