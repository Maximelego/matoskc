namespace MatosKC.Infrastructure.Persistence.Repositories;

using MatosKC.Application.Inspections.Ports;
using MatosKC.Domain.Entities.Inspection;
using Microsoft.EntityFrameworkCore;

public sealed class InspectionTemplatePhotoRepository(MatosKCDbContext db) : IInspectionTemplatePhotoRepository
{
    public Task<InspectionTemplatePhoto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.InspectionTemplatePhotos.AsNoTracking().SingleOrDefaultAsync(photo => photo.Id == id, ct);

    public async Task AddAsync(InspectionTemplatePhoto photo, CancellationToken ct = default) =>
        await db.InspectionTemplatePhotos.AddAsync(photo, ct);
}
