namespace MatosKC.Application.Tests.EquipmentCategories.List;

using MatosKC.Application.EquipmentCategories.List;
using MatosKC.Application.Tests.Fakes;
using MatosKC.Domain.Equipments;

public sealed class ListEquipmentCategoryUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnMappedCategories()
    {
        var category1 = new EquipmentCategory("Fenwick", "Manutention");
        var category2 = new EquipmentCategory("Souffleur", null);
        var repository = new FakeEquipmentCategoryRepository();
        repository.Seed(category1);
        repository.Seed(category2);
        var useCase = new ListEquipmentCategoryUseCase(repository);
        var query = new ListEquipmentCategoryQuery();

        ListEquipmentCategoryResult result = await useCase.ExecuteAsync(
            query,
            CancellationToken.None
        );

        Assert.Collection(
            result.EquipmentCategories.OrderBy(category => category.Name),
            category =>
            {
                Assert.Equal(category1.Id, category.Id);
                Assert.Equal(category1.Name, category.Name);
                Assert.Equal(category1.Description, category.Description);
            },
            category =>
            {
                Assert.Equal(category2.Id, category.Id);
                Assert.Equal(category2.Name, category.Name);
                Assert.Equal(category2.Description, category.Description);
            }
        );
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryIsEmpty_ShouldReturnEmptyList()
    {
        var repository = new FakeEquipmentCategoryRepository();
        var useCase = new ListEquipmentCategoryUseCase(repository);

        ListEquipmentCategoryResult result = await useCase.ExecuteAsync(
            new ListEquipmentCategoryQuery(),
            CancellationToken.None
        );

        Assert.Empty(result.EquipmentCategories);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateQueryAndCancellationToken()
    {
        var repository = new FakeEquipmentCategoryRepository();
        var useCase = new ListEquipmentCategoryUseCase(repository);
        var query = new ListEquipmentCategoryQuery();
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(query, cancellationToken);

        Assert.Same(query, repository.LastListQuery);
        Assert.Equal(cancellationToken, repository.ListCancellationToken);
    }
}
