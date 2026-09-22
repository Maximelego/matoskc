namespace MatosKC.Application.Equipments.Create;

using MatosKC.Application.EquipmentCategories.Get.Exceptions;
using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Application.Equipments.Create.Exceptions;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Domain.Equipments;

public class CreateEquipmentUseCase
{

    private readonly IEquipmentRepository EquipmentRepository;
    private readonly IEquipmentCategoryRepository EquipmentCategoryRepository;

    public CreateEquipmentUseCase(
        IEquipmentRepository equipmentRepository,
        IEquipmentCategoryRepository equipmentCategoryRepository)
    {
        EquipmentRepository = equipmentRepository;
        EquipmentCategoryRepository = equipmentCategoryRepository;
    }

    public async Task<Guid> ExecuteAsync(CreateEquipmentDto dto, CancellationToken cancellationToken = default)
    {
        var equipmentToCreate = new Equipment(
            dto.Name,
            dto.CategoryId,
            dto.SerialNumber
        );

        if (!await EquipmentCategoryRepository.ExistsByIdAsync(equipmentToCreate.CategoryId, cancellationToken))
        {
            throw new EquipmentCategoryNotFoundException(equipmentToCreate.CategoryId);
        }
        if (await EquipmentRepository.ExistsBySerialNumberAsync(equipmentToCreate.SerialNumber, cancellationToken))
        {
            throw new EquipmentSerialNumberAlreadyExistsException(equipmentToCreate.SerialNumber);
        }

        await EquipmentRepository.AddAsync(equipmentToCreate, cancellationToken);

        return equipmentToCreate.Id;
    }
}
