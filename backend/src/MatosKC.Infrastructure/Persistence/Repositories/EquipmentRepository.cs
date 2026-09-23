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
        throw new NotImplementedException();
    }

    public Task<Equipment> RetrieveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
