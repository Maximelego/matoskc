using MatosKC.Application.EquipmentCategories.Create;
using MatosKC.Application.EquipmentCategories.Create.Exceptions;
using MatosKC.Application.Tests.Fakes;

namespace MatosKC.Application.Tests.EquipmentCategories.Create;

public sealed class CreateEquipmentCategoryUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidData_ShouldAddCategoryAndReturnItsId()
    {
        var repository = new FakeEquipmentCategoryRepository();
        var useCase = new CreateEquipmentCategoryUseCase(repository);
        var dto = new CreateEquipmentCategoryDto(
            "Fenwick",
            "Chariots élévateurs");

        var result = await useCase.ExecuteAsync(dto);

        var addedCategory = Assert.Single(repository.AddedCategories);
        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(addedCategory.Id, result);
        Assert.Equal("Fenwick", addedCategory.Name);
        Assert.Equal("Chariots élévateurs", addedCategory.Description);
    }

    [Fact]
    public async Task ExecuteAsync_WithSurroundingSpaces_ShouldCheckAndStoreNormalizedValues()
    {
        var repository = new FakeEquipmentCategoryRepository();
        var useCase = new CreateEquipmentCategoryUseCase(repository);
        var dto = new CreateEquipmentCategoryDto(
            "  Fenwick  ",
            "  Chariots élévateurs  ");

        await useCase.ExecuteAsync(dto);

        var addedCategory = Assert.Single(repository.AddedCategories);
        Assert.Equal("Fenwick", repository.LastCheckedName);
        Assert.Equal("Fenwick", addedCategory.Name);
        Assert.Equal("Chariots élévateurs", addedCategory.Description);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameAlreadyExists_ShouldThrowAndNotAddCategory()
    {
        var repository = new FakeEquipmentCategoryRepository
        {
            NameAlreadyExists = true
        };
        var useCase = new CreateEquipmentCategoryUseCase(repository);
        var dto = new CreateEquipmentCategoryDto("Fenwick", null);

        await Assert.ThrowsAsync<EquipmentCategoryAlreadyExistsException>(
            () => useCase.ExecuteAsync(dto));

        Assert.Empty(repository.AddedCategories);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateCancellationTokenToRepository()
    {
        var repository = new FakeEquipmentCategoryRepository();
        var useCase = new CreateEquipmentCategoryUseCase(repository);
        var dto = new CreateEquipmentCategoryDto("Fenwick", null);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(dto, cancellationToken);

        Assert.Equal(cancellationToken, repository.ExistsByNameCancellationToken);
        Assert.Equal(cancellationToken, repository.AddCancellationToken);
    }
}

internal class EquipmentCategoryNameAlreadyExistsException
{
}
