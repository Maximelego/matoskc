namespace MatosKC.Infrastructure.Tests.Persistence.Repositories;

using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Persistence.Repositories;

[Collection(InfrastructureTestCollection.Name)]
public sealed class EquipmentRepositoryTests
{
    private readonly PostgreSqlFixture Fixture;

    public EquipmentRepositoryTests(PostgreSqlFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEquipmentAndMakeItRetrievable()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var categoryRepository = new EquipmentCategoryRepository(dbContext);
        var repository = new EquipmentRepository(dbContext);
        var category = new EquipmentCategory($"Category-{Guid.NewGuid():N}", null);
        var equipment = new Equipment(
            "Souffleur ISOVER",
            category.Id,
            $"SN-{Guid.NewGuid():N}"
        );
        await categoryRepository.AddAsync(category, TestContext.Current.CancellationToken);

        await repository.AddAsync(equipment, TestContext.Current.CancellationToken);

        Equipment? stored = await repository.RetrieveByIdAsync(
            equipment.Id,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(stored);
        Assert.Equal(equipment.Id, stored.Id);
        Assert.True(await repository.ExistsByIdAsync(
            equipment.Id,
            TestContext.Current.CancellationToken
        ));
        Assert.True(await repository.ExistsBySerialNumberAsync(
            equipment.SerialNumber,
            TestContext.Current.CancellationToken
        ));
    }

    [Fact]
    public async Task ListEquipmentsAsync_ShouldFilterByCategory()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var categoryRepository = new EquipmentCategoryRepository(dbContext);
        var repository = new EquipmentRepository(dbContext);
        var selectedCategory = new EquipmentCategory($"Category-{Guid.NewGuid():N}", null);
        var otherCategory = new EquipmentCategory($"Category-{Guid.NewGuid():N}", null);
        await categoryRepository.AddAsync(selectedCategory, TestContext.Current.CancellationToken);
        await categoryRepository.AddAsync(otherCategory, TestContext.Current.CancellationToken);
        var selected = new Equipment("Selected", selectedCategory.Id, $"SN-{Guid.NewGuid():N}");
        var excluded = new Equipment("Excluded", otherCategory.Id, $"SN-{Guid.NewGuid():N}");
        await repository.AddAsync(selected, TestContext.Current.CancellationToken);
        await repository.AddAsync(excluded, TestContext.Current.CancellationToken);

        List<Equipment> result = await repository.ListEquipmentsAsync(
            selectedCategory.Id,
            null,
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Contains(result, equipment => equipment.Id == selected.Id);
        Assert.DoesNotContain(result, equipment => equipment.Id == excluded.Id);
    }

    [Fact]
    public async Task ListEquipmentsAsync_ShouldFilterByStatus()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var categoryRepository = new EquipmentCategoryRepository(dbContext);
        var repository = new EquipmentRepository(dbContext);
        var category = new EquipmentCategory($"Category-{Guid.NewGuid():N}", null);
        await categoryRepository.AddAsync(category, TestContext.Current.CancellationToken);
        var borrowed = new Equipment("Borrowed", category.Id, $"SN-{Guid.NewGuid():N}");
        borrowed.ChangeStatus(EquipmentStatus.Borrowed);
        var available = new Equipment("Available", category.Id, $"SN-{Guid.NewGuid():N}");
        await repository.AddAsync(borrowed, TestContext.Current.CancellationToken);
        await repository.AddAsync(available, TestContext.Current.CancellationToken);

        List<Equipment> result = await repository.ListEquipmentsAsync(
            null,
            EquipmentStatus.Borrowed,
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Contains(result, equipment => equipment.Id == borrowed.Id);
        Assert.DoesNotContain(result, equipment => equipment.Id == available.Id);
    }

    [Fact]
    public async Task ListEquipmentsAsync_ShouldSearchNameAndSerialNumber()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var categoryRepository = new EquipmentCategoryRepository(dbContext);
        var repository = new EquipmentRepository(dbContext);
        var category = new EquipmentCategory($"Category-{Guid.NewGuid():N}", null);
        await categoryRepository.AddAsync(category, TestContext.Current.CancellationToken);
        string marker = Guid.NewGuid().ToString("N");
        var byName = new Equipment($"Machine-{marker}", category.Id, $"SN-{Guid.NewGuid():N}");
        var bySerial = new Equipment("Other machine", category.Id, $"SN-{marker}");
        var excluded = new Equipment("Excluded", category.Id, $"SN-{Guid.NewGuid():N}");
        await repository.AddAsync(byName, TestContext.Current.CancellationToken);
        await repository.AddAsync(bySerial, TestContext.Current.CancellationToken);
        await repository.AddAsync(excluded, TestContext.Current.CancellationToken);

        List<Equipment> result = await repository.ListEquipmentsAsync(
            null,
            null,
            marker,
            TestContext.Current.CancellationToken
        );

        Assert.Contains(result, equipment => equipment.Id == byName.Id);
        Assert.Contains(result, equipment => equipment.Id == bySerial.Id);
        Assert.DoesNotContain(result, equipment => equipment.Id == excluded.Id);
    }
}
