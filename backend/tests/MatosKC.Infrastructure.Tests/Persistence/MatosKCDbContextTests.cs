namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;

[Collection(InfrastructureTestCollection.Name)]
public sealed class MatosKCDbContextTests
{
    private readonly PostgreSqlFixture Fixture;

    public MatosKCDbContextTests(PostgreSqlFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task EquipmentCategory_ShouldBePersistedAndReadBack()
    {
        await using var writeContext = Fixture.CreateDbContext();
        var category = new EquipmentCategory(
            $"Fenwick-{Guid.NewGuid():N}",
            "Matériel de manutention"
        );

        writeContext.Set<EquipmentCategory>().Add(category);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var readContext = Fixture.CreateDbContext();
        EquipmentCategory? storedCategory = await readContext
            .Set<EquipmentCategory>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == category.Id,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(storedCategory);
        Assert.Equal(category.Id, storedCategory.Id);
        Assert.Equal(category.Name, storedCategory.Name);
        Assert.Equal(category.Description, storedCategory.Description);
    }

    [Fact]
    public async Task Equipment_ShouldPreserveItsCategorySerialNumberAndStatus()
    {
        await using var writeContext = Fixture.CreateDbContext();
        var category = new EquipmentCategory(
            $"Souffleur-{Guid.NewGuid():N}",
            null
        );
        var equipment = new Equipment(
            "Souffleur ISOVER",
            category.Id,
            $"SN-{Guid.NewGuid():N}"
        );

        writeContext.Set<EquipmentCategory>().Add(category);
        writeContext.Set<Equipment>().Add(equipment);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var readContext = Fixture.CreateDbContext();
        Equipment? storedEquipment = await readContext
            .Set<Equipment>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == equipment.Id,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(storedEquipment);
        Assert.Equal(equipment.Id, storedEquipment.Id);
        Assert.Equal(equipment.Name, storedEquipment.Name);
        Assert.Equal(category.Id, storedEquipment.CategoryId);
        Assert.Equal(equipment.SerialNumber, storedEquipment.SerialNumber);
        Assert.Equal(EquipmentStatus.Available, storedEquipment.Status);
    }

    [Fact]
    public async Task Equipment_WithUnknownCategory_ShouldViolateForeignKey()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var equipment = new Equipment(
            "Matériel sans catégorie",
            Guid.NewGuid(),
            $"SN-{Guid.NewGuid():N}"
        );

        dbContext.Set<Equipment>().Add(equipment);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync(TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task EquipmentPhoto_ShouldBePersistedAndDeletedWithItsEquipment()
    {
        Guid photoId = Guid.NewGuid();

        await using (var writeContext = Fixture.CreateDbContext())
        {
            var category = new EquipmentCategory(
                $"Photos-{Guid.NewGuid():N}",
                null
            );
            var equipment = new Equipment(
                "Souffleur photographié",
                category.Id,
                $"PHOTO-{Guid.NewGuid():N}"
            );
            var photo = new EquipmentPhoto(
                photoId,
                equipment.Id,
                $"equipments/{equipment.Id}/photos/{photoId}",
                "souffleur.png",
                "image/png",
                68,
                DateTimeOffset.UtcNow
            );

            writeContext.AddRange(category, equipment, photo);
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            writeContext.Equipments.Remove(equipment);
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = Fixture.CreateDbContext();
        Assert.False(await readContext.EquipmentPhotos.AnyAsync(
            photo => photo.Id == photoId,
            TestContext.Current.CancellationToken
        ));
    }
}
