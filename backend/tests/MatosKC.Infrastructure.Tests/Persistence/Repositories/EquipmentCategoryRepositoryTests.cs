namespace MatosKC.Infrastructure.Tests.Persistence.Repositories;

using MatosKC.Application.EquipmentCategories.List;
using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Persistence.Repositories;

[Collection(InfrastructureTestCollection.Name)]
public sealed class EquipmentCategoryRepositoryTests
{
    private readonly PostgreSqlFixture Fixture;

    public EquipmentCategoryRepositoryTests(PostgreSqlFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistCategory()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var repository = new EquipmentCategoryRepository(dbContext);
        var category = new EquipmentCategory(
            $"Category-{Guid.NewGuid():N}",
            "Description"
        );

        await repository.AddAsync(
            category,
            TestContext.Current.CancellationToken
        );

        EquipmentCategory? stored = await repository.GetByIdAsync(
            category.Id,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(stored);
        Assert.Equal(category.Id, stored.Id);
        Assert.Equal(category.Name, stored.Name);
        Assert.Equal(category.Description, stored.Description);
    }

    [Fact]
    public async Task ExistsMethods_WithStoredCategory_ShouldReturnTrue()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var repository = new EquipmentCategoryRepository(dbContext);
        var category = new EquipmentCategory(
            $"Category-{Guid.NewGuid():N}",
            null
        );
        await repository.AddAsync(category, TestContext.Current.CancellationToken);

        bool existsById = await repository.ExistsByIdAsync(
            category.Id,
            TestContext.Current.CancellationToken
        );
        bool existsByName = await repository.ExistsByNameAsync(
            category.Name,
            TestContext.Current.CancellationToken
        );

        Assert.True(existsById);
        Assert.True(existsByName);
    }

    [Fact]
    public async Task ListEquipmentCategoriesAsync_ShouldContainStoredCategories()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var repository = new EquipmentCategoryRepository(dbContext);
        var first = new EquipmentCategory($"Category-{Guid.NewGuid():N}", null);
        var second = new EquipmentCategory($"Category-{Guid.NewGuid():N}", null);
        await repository.AddAsync(first, TestContext.Current.CancellationToken);
        await repository.AddAsync(second, TestContext.Current.CancellationToken);

        List<EquipmentCategory> categories =
            await repository.ListEquipmentCategoriesAsync(
                new ListEquipmentCategoryQuery(),
                TestContext.Current.CancellationToken
            );

        Assert.Contains(categories, category => category.Id == first.Id);
        Assert.Contains(categories, category => category.Id == second.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ShouldReturnNull()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var repository = new EquipmentCategoryRepository(dbContext);

        EquipmentCategory? category = await repository.GetByIdAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        Assert.Null(category);
    }
}
