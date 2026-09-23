

using MatosKC.Application.EquipmentCategories.Get;
using MatosKC.Application.EquipmentCategories.Get.Exceptions;
using MatosKC.Application.Tests.Fakes;
using MatosKC.Domain.Equipments;

namespace MatosKC.Application.Tests.EquipmentCategories.Get;

public sealed class GetEquipmentCategoryUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCategoryExists_ShouldReturnCategoryResult()
    {
        var category = new EquipmentCategory(
            Guid.NewGuid(),
            "Fenwick",
            "Chariots élévateurs");
        var repository = new FakeEquipmentCategoryRepository();
        repository.Seed(category);
        var useCase = new GetEquipmentCategoryUseCase(repository);

        GetEquipmentCategoryResult result = await useCase.ExecuteAsync(category.Id);

        Assert.Equal(category.Id, result.Id);
        Assert.Equal(category.Name, result.Name);
        Assert.Equal(category.Description, result.Description);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryDoesNotExist_ShouldThrow()
    {
        var repository = new FakeEquipmentCategoryRepository();
        var useCase = new GetEquipmentCategoryUseCase(repository);
        var unknownId = Guid.NewGuid();

        await Assert.ThrowsAsync<EquipmentCategoryNotFoundException>(
            () => useCase.ExecuteAsync(unknownId));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateCancellationTokenToRepository()
    {
        var category = new EquipmentCategory("Fenwick", null);
        var repository = new FakeEquipmentCategoryRepository();
        repository.Seed(category);
        var useCase = new GetEquipmentCategoryUseCase(repository);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(category.Id, cancellationToken);

        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
    }
}
