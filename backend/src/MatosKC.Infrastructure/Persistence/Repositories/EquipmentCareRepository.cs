namespace MatosKC.Infrastructure.Persistence.Repositories;

using MatosKC.Application.Maintenance;
using MatosKC.Domain.Entities.Maintenance;
using Microsoft.EntityFrameworkCore;

public sealed class EquipmentCareRepository(MatosKCDbContext db) : IEquipmentCareRepository
{
    public Task<EquipmentDefect?> GetDefectAsync(Guid id, CancellationToken ct) =>
        db.EquipmentDefects.SingleOrDefaultAsync(defect => defect.Id == id, ct);

    public async Task AddDefectAsync(EquipmentDefect defect, CancellationToken ct) =>
        await db.EquipmentDefects.AddAsync(defect, ct);

    public async Task AddOccurrenceAsync(DefectOccurrence occurrence, CancellationToken ct) =>
        await db.DefectOccurrences.AddAsync(occurrence, ct);

    public async Task<IReadOnlyList<DefectOccurrence>> GetOccurrencesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await db.DefectOccurrences.AsNoTracking().Where(occurrence => ids.Contains(occurrence.DefectId))
            .OrderBy(occurrence => occurrence.ObservedAt).ThenBy(occurrence => occurrence.Id).ToListAsync(ct);

    public async Task<CarePage<EquipmentDefect>> ListDefectsAsync(Guid equipmentId, EquipmentDefectStatus? status,
        int offset, int limit, CancellationToken ct)
    {
        var query = db.EquipmentDefects.AsNoTracking().Where(defect => defect.EquipmentId == equipmentId);

        if (status.HasValue)
        {
            query = query.Where(defect => defect.Status == status.Value);
        }

        int total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(defect => defect.ReportedAt).ThenByDescending(defect => defect.Id)
            .Skip(offset).Take(limit).ToListAsync(ct);

        return new(items, total, offset, limit);
    }

    public Task<EquipmentMaintenance?> GetMaintenanceAsync(Guid id, CancellationToken ct) =>
        db.EquipmentMaintenances.AsNoTracking().SingleOrDefaultAsync(maintenance => maintenance.Id == id, ct);

    public async Task AddMaintenanceAsync(EquipmentMaintenance maintenance, CancellationToken ct) =>
        await db.EquipmentMaintenances.AddAsync(maintenance, ct);

    public async Task<CarePage<EquipmentMaintenance>> ListMaintenancesAsync(Guid equipmentId,
        int offset, int limit, CancellationToken ct)
    {
        var query = db.EquipmentMaintenances.AsNoTracking().Where(maintenance => maintenance.EquipmentId == equipmentId);
        int total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(maintenance => maintenance.PerformedAt).ThenByDescending(maintenance => maintenance.Id)
            .Skip(offset).Take(limit).ToListAsync(ct);

        return new(items, total, offset, limit);
    }
}
