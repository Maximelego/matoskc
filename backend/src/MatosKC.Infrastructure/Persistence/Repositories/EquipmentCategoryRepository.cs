namespace MatosKC.Infrastructure.Persistence.Repositories;

using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;

public sealed class EquipmentCategoryRepository
    : IEquipmentCategoryRepository
{
    private readonly MatosKCDbContext _dbContext;

    public EquipmentCategoryRepository(
        MatosKCDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.EquipmentCategories.AnyAsync(
            category => category.Id == id,
            cancellationToken
        );
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        return _dbContext.EquipmentCategories.AnyAsync(
            category => category.Name == name,
            cancellationToken
        );
    }

    public Task<EquipmentCategory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.EquipmentCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                category => category.Id == id,
                cancellationToken
            );
    }

    public async Task AddAsync(
        EquipmentCategory category,
        CancellationToken cancellationToken)
    {
        await _dbContext.EquipmentCategories.AddAsync(
            category,
            cancellationToken
        );

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );
    }

    public Task<EquipmentCategory?> GetEquipmentCategoryByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
