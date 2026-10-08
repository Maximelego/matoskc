using Microsoft.EntityFrameworkCore;
namespace MatosKC.Infrastructure.Persistence.Repositories;

using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Domain.Equipments;

public sealed class EquipmentPhotoRepository : IEquipmentPhotoRepository
{
    private readonly MatosKCDbContext DbContext;

    public EquipmentPhotoRepository(MatosKCDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task AddAsync(
        EquipmentPhoto photo,
        CancellationToken cancellationToken
    )
    {
        await DbContext.EquipmentPhotos.AddAsync(photo, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<EquipmentPhoto?> RetrieveByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return DbContext.EquipmentPhotos.SingleOrDefaultAsync(
            photo => photo.Id == id,
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<EquipmentPhoto>> ListByEquipmentIdAsync(
        Guid equipmentId,
        CancellationToken cancellationToken
    )
    {
        return await DbContext.EquipmentPhotos
            .AsNoTracking()
            .Where(photo => photo.EquipmentId == equipmentId)
            .OrderBy(photo => photo.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(
        EquipmentPhoto photo,
        CancellationToken cancellationToken
    )
    {
        DbContext.EquipmentPhotos.Update(photo);
        return DbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(
        EquipmentPhoto photo,
        CancellationToken cancellationToken
    )
    {
        DbContext.EquipmentPhotos.Remove(photo);
        return DbContext.SaveChangesAsync(cancellationToken);
    }
}
