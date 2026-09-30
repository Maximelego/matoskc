namespace MatosKC.Application.Tests.Equipments.Get;

using MatosKC.Application.Equipments.Get;
using MatosKC.Application.Equipments.Get.Exceptions;
using MatosKC.Application.Tests.Fakes;
using MatosKC.Domain.Equipments;

public sealed class GetEquipmentUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEquipmentExists_ShouldReturnItsDto()
    {
        var equipment = new Equipment(
            "Fenwick 01",
            Guid.NewGuid(),
            "SN-123"
        );
        var repository = new FakeEquipmentRepository();
        repository.Seed(equipment);
        var useCase = new GetEquipmentUseCase(repository);

        GetEquipmentDto result = await useCase.ExecuteAsync(
            equipment.Id,
            CancellationToken.None
        );

        Assert.Equal(equipment.Id, result.Id);
        Assert.Equal(equipment.Name, result.Name);
        Assert.Equal(equipment.SerialNumber, result.SerialNumber);
        Assert.Equal(equipment.CategoryId, result.EquipmentCategoryId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEquipmentDoesNotExist_ShouldThrow()
    {
        var repository = new FakeEquipmentRepository();
        var useCase = new GetEquipmentUseCase(repository);
        Guid unknownId = Guid.NewGuid();

        await Assert.ThrowsAsync<EquipmentNotFoundException>(
            () => useCase.ExecuteAsync(
                unknownId,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateIdAndCancellationToken()
    {
        var equipment = new Equipment(
            "Fenwick 01",
            Guid.NewGuid(),
            "SN-123"
        );
        var repository = new FakeEquipmentRepository();
        repository.Seed(equipment);
        var useCase = new GetEquipmentUseCase(repository);
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(equipment.Id, cancellationToken);

        Assert.Equal(equipment.Id, repository.LastRequestedId);
        Assert.Equal(cancellationToken, repository.RetrieveCancellationToken);
    }
}
