namespace MatosKC.Infrastructure.Persistence.Seeding;

using MatosKC.Domain.Equipments;

using Microsoft.EntityFrameworkCore;

public sealed class DevelopmentDataSeeder
{
    private readonly MatosKCDbContext DbContext;

    public DevelopmentDataSeeder(MatosKCDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<EquipmentCategory> categories =
            DevelopmentData.CreateCategories();

        Guid[] categoryIds = categories
            .Select(category => category.Id)
            .ToArray();

        HashSet<Guid> existingCategoryIds = await DbContext.EquipmentCategories
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .Select(category => category.Id)
            .ToHashSetAsync(cancellationToken);

        await DbContext.EquipmentCategories.AddRangeAsync(
            categories.Where(category => !existingCategoryIds.Contains(category.Id)),
            cancellationToken
        );

        await DbContext.SaveChangesAsync(cancellationToken);

        IReadOnlyCollection<Equipment> equipments =
            DevelopmentData.CreateEquipments();

        Guid[] equipmentIds = equipments
            .Select(equipment => equipment.Id)
            .ToArray();

        HashSet<Guid> existingEquipmentIds = await DbContext.Equipments
            .AsNoTracking()
            .Where(equipment => equipmentIds.Contains(equipment.Id))
            .Select(equipment => equipment.Id)
            .ToHashSetAsync(cancellationToken);

        await DbContext.Equipments.AddRangeAsync(
            equipments.Where(equipment => !existingEquipmentIds.Contains(equipment.Id)),
            cancellationToken
        );

        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
