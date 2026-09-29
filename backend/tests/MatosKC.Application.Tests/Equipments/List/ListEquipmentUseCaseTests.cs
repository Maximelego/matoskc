namespace MatosKC.Application.Tests.Equipments.List;

using MatosKC.Application.Equipments.List;
using MatosKC.Application.Tests.Fakes;
using MatosKC.Domain.Equipments;

public sealed class ListEquipmentUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnMappedFilteredEquipments()
    {
        Guid selectedCategoryId = Guid.NewGuid();
        var expected = new Equipment(
            "Fenwick 01",
            selectedCategoryId,
            "SN-123"
        );
        var excluded = new Equipment(
            "Souffleur",
            Guid.NewGuid(),
            "SN-456"
        );
        var repository = new FakeEquipmentRepository();
        repository.Seed(expected);
        repository.Seed(excluded);
        var useCase = new ListEquipmentUseCase(repository);
        var query = new ListEquipmentsQuery(
            selectedCategoryId,
            EquipmentStatus.Available,
            "Fenwick"
        );

        ListEquipmentResult result = await useCase.ExecuteAsync(
            query,
            CancellationToken.None
        );

        var equipment = Assert.Single(result.Equipments);
        Assert.Equal(expected.Id, equipment.Id);
        Assert.Equal(expected.Name, equipment.Name);
        Assert.Equal(expected.SerialNumber, equipment.SerialNumber);
        Assert.Equal(expected.CategoryId, equipment.EquipmentCategoryId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoEquipmentMatches_ShouldReturnEmptyList()
    {
        var repository = new FakeEquipmentRepository();
        repository.Seed(
            new Equipment("Fenwick", Guid.NewGuid(), "SN-123")
        );
        var useCase = new ListEquipmentUseCase(repository);

        ListEquipmentResult result = await useCase.ExecuteAsync(
            new ListEquipmentsQuery(null, null, "unknown"),
            CancellationToken.None
        );

        Assert.Empty(result.Equipments);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateFiltersAndCancellationToken()
    {
        var repository = new FakeEquipmentRepository();
        var useCase = new ListEquipmentUseCase(repository);
        Guid categoryId = Guid.NewGuid();
        var query = new ListEquipmentsQuery(
            categoryId,
            EquipmentStatus.Maintenance,
            "SN-"
        );
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(query, cancellationToken);

        Assert.Equal(categoryId, repository.LastCategoryIdFilter);
        Assert.Equal(EquipmentStatus.Maintenance, repository.LastStatusFilter);
        Assert.Equal("SN-", repository.LastSearchFilter);
        Assert.Equal(cancellationToken, repository.ListCancellationToken);
    }
}
