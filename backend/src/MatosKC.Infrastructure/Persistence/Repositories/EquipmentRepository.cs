namespace MatosKC.Infrastructure.Persistence.Repositories;

using MatosKC.Application.Equipments.Ports;
using MatosKC.Domain.Equipments;

using Microsoft.EntityFrameworkCore;

public sealed class EquipmentRepository
    : IEquipmentRepository
{
    private readonly MatosKCDbContext _dbContext;

    public EquipmentRepository(
        MatosKCDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsBySerialNumberAsync(
        string serialNumber,
        CancellationToken cancellationToken)
    {
        return _dbContext.Equipments.AnyAsync(
            equipment =>
                equipment.SerialNumber == serialNumber,
            cancellationToken
        );
    }

    public async Task AddAsync(
        Equipment equipment,
        CancellationToken cancellationToken)
    {
        await _dbContext.Equipments.AddAsync(
            equipment,
            cancellationToken
        );

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );
    }

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Equipments.AnyAsync(
            equipment =>
                equipment.Id == id,
            cancellationToken
        );
    }

    public Task<Equipment?> RetrieveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Equipments
            .SingleOrDefaultAsync(
                equipment => equipment.Id == id,
                cancellationToken
            );
    }

    public Task<List<Equipment>> ListEquipmentsAsync(Guid? categoryId, EquipmentStatus? status, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Equipments.AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(equipment => equipment.CategoryId == categoryId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(equipment => equipment.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(equipment =>
                equipment.Name.Contains(search) ||
                equipment.SerialNumber.Contains(search));
        }

        return query.ToListAsync(cancellationToken);
    }
}
