using Microsoft.EntityFrameworkCore;
namespace MatosKC.Infrastructure.Persistence.Seeding;

using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Equipments;

public sealed class DevelopmentDataSeeder
{
    private readonly MatosKCDbContext DbContext;
    private readonly IObjectStorage ObjectStorage;

    private const string PlaceholderPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    public DevelopmentDataSeeder(
        MatosKCDbContext dbContext,
        IObjectStorage objectStorage
    )
    {
        DbContext = dbContext;
        ObjectStorage = objectStorage;
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

        await SeedPhotosAsync(cancellationToken);
    }

    private async Task SeedPhotosAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<EquipmentPhoto> photos =
            DevelopmentData.CreateEquipmentPhotos();
        Guid[] photoIds = photos.Select(photo => photo.Id).ToArray();

        HashSet<Guid> existingPhotoIds = await DbContext.EquipmentPhotos
            .AsNoTracking()
            .Where(photo => photoIds.Contains(photo.Id))
            .Select(photo => photo.Id)
            .ToHashSetAsync(cancellationToken);

        byte[] content = Convert.FromBase64String(PlaceholderPngBase64);
        await ObjectStorage.EnsureReadyAsync(cancellationToken);

        foreach (EquipmentPhoto photo in photos.Where(
                     photo => !existingPhotoIds.Contains(photo.Id)))
        {
            await using var stream = new MemoryStream(content, writable: false);
            await ObjectStorage.StoreAsync(
                photo.ObjectKey,
                stream,
                photo.ContentType,
                cancellationToken
            );

            await DbContext.EquipmentPhotos.AddAsync(photo, cancellationToken);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
