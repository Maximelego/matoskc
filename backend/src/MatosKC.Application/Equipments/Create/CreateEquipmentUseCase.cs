namespace MatosKC.Application.Equipments.Create;

using MatosKC.Application.Equipment.Ports;
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

    public async Task CreateEquipment(CreateEquipmentDto dto)
    {

        if (!await EquipmentCategoryRepository.ExistsByIdAsync(dto.CategoryId, new CancellationToken()))
        {
            throw new EquipmentCategoryNotFoundException(dto.CategoryId);
        }
        if (await EquipmentRepository.ExistsBySerialNumberAsync(dto.SerialNumber, new CancellationToken()))
        {
            throw new EquipmentSerialNumberAlreadyExistsException(dto.SerialNumber);
        }

        var equipmentToCreate = new Equipment(
            dto.Name,
            dto.CategoryId,
            dto.SerialNumber
        );

        await EquipmentRepository.AddAsync(equipmentToCreate, new CancellationToken());
    }
}
