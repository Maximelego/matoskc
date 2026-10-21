namespace MatosKC.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using MatosKC.Application.Clients.Ports;
using MatosKC.Application.Rentals.Ports;
using MatosKC.Application.Inspections.Ports;
using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

// These repositories stage changes; EfUnitOfWork owns the commit.
public sealed class ClientRepository(MatosKCDbContext db) : IClientRepository
{
    public Task<Client?> GetByIdAsync(Guid clientId, CancellationToken ct = default) =>
        db.Clients.SingleOrDefaultAsync(client => client.Id == clientId, ct);

    public async Task AddClientAsync(Client client, CancellationToken ct = default) =>
        await db.Clients.AddAsync(client, ct);
}

public sealed class RentalRepository(MatosKCDbContext db) : IRentalRepository
{
    public Task<Rental?> GetByIdAsync(Guid rentalId, CancellationToken ct = default) =>
        db.Rentals.SingleOrDefaultAsync(rental => rental.Id == rentalId, ct);

    public Task<Rental?> GetActiveForEquipmentAsync(Guid equipmentId, CancellationToken ct = default) =>
        db.Rentals.SingleOrDefaultAsync(rental => rental.EquipmentId == equipmentId && rental.Status == RentalStatus.Active, ct);

    public Task<Rental?> GetOpenForEquipmentAsync(Guid equipmentId, CancellationToken ct = default) =>
        db.Rentals.SingleOrDefaultAsync(rental => rental.EquipmentId == equipmentId
            && (rental.Status == RentalStatus.Active || rental.Status == RentalStatus.Draft), ct);

    public async Task AddAsync(Rental rental, CancellationToken ct = default) =>
        await db.Rentals.AddAsync(rental, ct);

    public Task UpdateAsync(Rental rental, CancellationToken ct = default)
    {
        if (db.Entry(rental).State == EntityState.Detached)
        {
            db.Rentals.Update(rental);
        }

        return Task.CompletedTask;
    }
}

public sealed class InspectionsRepository(MatosKCDbContext db) : IInspectionsRepository
{
    private IQueryable<InspectionEntity> Complete => db.Inspections.Include(inspection => inspection.Answers)
        .Include(inspection => inspection.KnownDefects).Include(inspection => inspection.Findings);

    public Task<InspectionEntity?> GetInspectionByIdAsync(Guid inspectionId, CancellationToken ct = default) =>
        Complete.SingleOrDefaultAsync(inspection => inspection.Id == inspectionId, ct);

    public Task<InspectionEntity?> GetForRentalAsync(Guid rentalId, InspectionType type, CancellationToken ct = default) =>
        Complete.SingleOrDefaultAsync(inspection => inspection.RentalId == rentalId && inspection.Type == type
            && inspection.Status != InspectionStatus.Cancelled, ct);

    public async Task<IReadOnlyList<InspectionEntity>> ListAsync(
        Guid? rentalId,
        InspectionType? type,
        InspectionStatus? status,
        int offset,
        int limit,
        CancellationToken ct = default
    )
    {
        var query = Complete;

        if (rentalId.HasValue)
        {
            query = query.Where(inspection => inspection.RentalId == rentalId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(inspection => inspection.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(inspection => inspection.Status == status.Value);
        }

        return await query.OrderByDescending(inspection => inspection.StartedAt)
            .ThenBy(inspection => inspection.Id).Skip(offset).Take(limit).ToArrayAsync(ct);
    }

    public async Task AddInspectionAsync(InspectionEntity inspection, CancellationToken ct = default) =>
        await db.Inspections.AddAsync(inspection, ct);

    public Task UpdateInspectionAsync(InspectionEntity inspection, CancellationToken ct = default)
    {
        if (db.Entry(inspection).State == EntityState.Detached)
        {
            db.Inspections.Update(inspection);
        }

        return Task.CompletedTask;
    }
}

public sealed class InspectionTemplateRepository(MatosKCDbContext db) : IInspectionTemplateRepository
{
    private IQueryable<InspectionTemplate> Complete => db.InspectionTemplates
        .Include(template => template.Points.OrderBy(point => point.Position).ThenBy(point => point.Id));

    public Task<InspectionTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken ct = default) =>
        Complete.AsNoTracking().SingleOrDefaultAsync(template => template.Id == templateId, ct);

    public Task<InspectionTemplate?> GetLatestForCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        Complete.AsNoTracking().Where(template => template.EquipmentCategoryId == categoryId)
            .OrderByDescending(template => template.CreatedAt).ThenByDescending(template => template.Id)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(InspectionTemplate template, CancellationToken ct = default) =>
        await db.InspectionTemplates.AddAsync(template, ct);
}

public sealed class InspectionMediaRepository(MatosKCDbContext db) : IInspectionMediaRepository
{
    public Task<InspectionMedia?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.InspectionMedia.SingleOrDefaultAsync(media => media.Id == id, ct);

    public async Task AddAsync(InspectionMedia media, CancellationToken ct = default) =>
        await db.InspectionMedia.AddAsync(media, ct);
}
