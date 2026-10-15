namespace MatosKC.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using MatosKC.Application.EquipmentPhotos.Ports;

public sealed class EquipmentThumbnailRepository(MatosKCDbContext db) : IEquipmentThumbnailRepository
{
    public async Task<IReadOnlyDictionary<Guid, EquipmentThumbnail>> GetAsync(
        IReadOnlyCollection<Guid> equipmentIds, CancellationToken ct)
    {
        var rows = await db.Equipments.AsNoTracking()
            .Where(equipment => equipmentIds.Contains(equipment.Id))
            .Select(equipment => new
            {
                equipment.Id,
                Photo = db.EquipmentPhotos.Where(photo => photo.EquipmentId == equipment.Id)
                    .OrderBy(photo => photo.CreatedAtUtc).ThenBy(photo => photo.Id)
                    .Select(photo => new EquipmentThumbnail(photo.Id, photo.UpdatedAtUtc))
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return rows.Where(row => row.Photo is not null)
            .ToDictionary(row => row.Id, row => row.Photo!);
    }
}
