using Microsoft.EntityFrameworkCore;
namespace MatosKC.Infrastructure.Persistence.Repositories;

using MatosKC.Application.EquipmentCategories.List;
using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Domain.Equipments;

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
        CancellationToken cancellationToken, Guid? agencyId = null)
    {
        return _dbContext.EquipmentCategories.AnyAsync(
            category => category.Name == name && (!agencyId.HasValue || category.AgencyId == agencyId),
            cancellationToken
        );
    }

    public Task<EquipmentCategory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.EquipmentCategories
            .SingleOrDefaultAsync(
                category => category.Id == id,
                cancellationToken
            );
    }

    public Task UpdateAsync(EquipmentCategory category, CancellationToken cancellationToken)
    {
        _dbContext.EquipmentCategories.Update(category);

        return Task.CompletedTask;
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

    public async Task<EquipmentCategory?> GetEquipmentCategoryByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.EquipmentCategories
            .SingleOrDefaultAsync(
                category => category.Id == id,
                cancellationToken
            );
    }

    public Task<List<EquipmentCategory>> ListEquipmentCategoriesAsync(ListEquipmentCategoryQuery query, CancellationToken cancellationToken)
    {
        return _dbContext.EquipmentCategories
            .ToListAsync(cancellationToken);
    }
}
