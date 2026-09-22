using MatosKC.Application.EquipmentCategories.Get.Exceptions;
using MatosKC.Application.Equipments.Create;
using MatosKC.Application.Equipments.Create.Exceptions;
using MatosKC.Application.Tests.Fakes;
using MatosKC.Domain.Equipments;

namespace MatosKC.Application.Tests.Equipments.Create;

public sealed class CreateEquipmentUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidData_ShouldAddAvailableEquipmentAndReturnItsId()
    {
        var category = new EquipmentCategory("Fenwick", null);
        var categoryRepository = new FakeEquipmentCategoryRepository();
        categoryRepository.Seed(category);
        var equipmentRepository = new FakeEquipmentRepository();
        var useCase = new CreateEquipmentUseCase(
            equipmentRepository,
            categoryRepository);
        var dto = new CreateEquipmentDto(
            "Fenwick 01",
            category.Id,
            "SN-123");

        var result = await useCase.ExecuteAsync(dto);

        var addedEquipment = Assert.Single(equipmentRepository.AddedEquipments);
        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(addedEquipment.Id, result);
        Assert.Equal("Fenwick 01", addedEquipment.Name);
        Assert.Equal(category.Id, addedEquipment.CategoryId);
        Assert.Equal("SN-123", addedEquipment.SerialNumber);
        Assert.Equal(EquipmentStatus.Available, addedEquipment.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithSurroundingSpaces_ShouldCheckAndStoreNormalizedValues()
    {
        var category = new EquipmentCategory("Fenwick", null);
        var categoryRepository = new FakeEquipmentCategoryRepository();
        categoryRepository.Seed(category);
        var equipmentRepository = new FakeEquipmentRepository();
        var useCase = new CreateEquipmentUseCase(
            equipmentRepository,
            categoryRepository);
        var dto = new CreateEquipmentDto(
            "  Fenwick 01  ",
            category.Id,
            "  SN-123  ");

        await useCase.ExecuteAsync(dto);

        var addedEquipment = Assert.Single(equipmentRepository.AddedEquipments);
        Assert.Equal("SN-123", equipmentRepository.LastCheckedSerialNumber);
        Assert.Equal("Fenwick 01", addedEquipment.Name);
        Assert.Equal("SN-123", addedEquipment.SerialNumber);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryDoesNotExist_ShouldThrowAndNotAddEquipment()
    {
        var categoryRepository = new FakeEquipmentCategoryRepository();
        var equipmentRepository = new FakeEquipmentRepository();
        var useCase = new CreateEquipmentUseCase(
            equipmentRepository,
            categoryRepository);
        var dto = new CreateEquipmentDto(
            "Fenwick 01",
            Guid.NewGuid(),
            "SN-123");

        await Assert.ThrowsAsync<EquipmentCategoryNotFoundException>(
            () => useCase.ExecuteAsync(dto));

        Assert.Empty(equipmentRepository.AddedEquipments);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSerialNumberAlreadyExists_ShouldThrowAndNotAddEquipment()
    {
        var category = new EquipmentCategory("Fenwick", null);
        var categoryRepository = new FakeEquipmentCategoryRepository();
        categoryRepository.Seed(category);
        var equipmentRepository = new FakeEquipmentRepository
        {
            SerialNumberAlreadyExists = true
        };
        var useCase = new CreateEquipmentUseCase(
            equipmentRepository,
            categoryRepository);
        var dto = new CreateEquipmentDto(
            "Fenwick 01",
            category.Id,
            "SN-123");

        await Assert.ThrowsAsync<EquipmentSerialNumberAlreadyExistsException>(
            () => useCase.ExecuteAsync(dto));

        Assert.Empty(equipmentRepository.AddedEquipments);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateCancellationTokenToRepositories()
    {
        var category = new EquipmentCategory("Fenwick", null);
        var categoryRepository = new FakeEquipmentCategoryRepository();
        categoryRepository.Seed(category);
        var equipmentRepository = new FakeEquipmentRepository();
        var useCase = new CreateEquipmentUseCase(
            equipmentRepository,
            categoryRepository);
        var dto = new CreateEquipmentDto(
            "Fenwick 01",
            category.Id,
            "SN-123");
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(dto, cancellationToken);

        Assert.Equal(cancellationToken, categoryRepository.ExistsByIdCancellationToken);
        Assert.Equal(cancellationToken, equipmentRepository.ExistsCancellationToken);
        Assert.Equal(cancellationToken, equipmentRepository.AddCancellationToken);
    }
}
