namespace MatosKC.Application.Tests.Fakes;

using MatosKC.Application.Maintenance;
using MatosKC.Domain.Entities.Maintenance;

internal sealed class FakeEquipmentCareRepository : IEquipmentCareRepository
{
    public List<EquipmentDefect> Defects { get; } = [];
    public List<DefectOccurrence> Occurrences { get; } = [];
    public List<EquipmentMaintenance> Maintenances { get; } = [];

    public Task<EquipmentDefect?> GetDefectAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Defects.SingleOrDefault(defect => defect.Id == id));

    public Task AddDefectAsync(EquipmentDefect defect, CancellationToken ct)
    {
        Defects.Add(defect);
        return Task.CompletedTask;
    }

    public Task AddOccurrenceAsync(DefectOccurrence occurrence, CancellationToken ct)
    {
        Occurrences.Add(occurrence);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DefectOccurrence>> GetOccurrencesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<DefectOccurrence>>(Occurrences.Where(occurrence => ids.Contains(occurrence.DefectId)).ToArray());

    public Task<CarePage<EquipmentDefect>> ListDefectsAsync(Guid equipmentId, EquipmentDefectStatus? status,
        int offset, int limit, CancellationToken ct)
    {
        var rows = Defects.Where(defect => defect.EquipmentId == equipmentId && (!status.HasValue || defect.Status == status)).ToArray();
        return Task.FromResult(new CarePage<EquipmentDefect>(rows.Skip(offset).Take(limit).ToArray(), rows.Length, offset, limit));
    }

    public Task<EquipmentMaintenance?> GetMaintenanceAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Maintenances.SingleOrDefault(maintenance => maintenance.Id == id));

    public Task AddMaintenanceAsync(EquipmentMaintenance maintenance, CancellationToken ct)
    {
        Maintenances.Add(maintenance);
        return Task.CompletedTask;
    }

    public Task<CarePage<EquipmentMaintenance>> ListMaintenancesAsync(Guid equipmentId, int offset, int limit, CancellationToken ct)
    {
        var rows = Maintenances.Where(maintenance => maintenance.EquipmentId == equipmentId).ToArray();
        return Task.FromResult(new CarePage<EquipmentMaintenance>(rows.Skip(offset).Take(limit).ToArray(), rows.Length, offset, limit));
    }
}
