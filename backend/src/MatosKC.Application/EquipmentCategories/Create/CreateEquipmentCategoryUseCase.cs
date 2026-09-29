namespace MatosKC.Application.EquipmentCategories.Create;

using MatosKC.Application.EquipmentCategories.Create.Exceptions;
using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Domain.Equipments;

public class CreateEquipmentCategoryUseCase
{
    private readonly IEquipmentCategoryRepository EquipmentCategoryRepository;

    public CreateEquipmentCategoryUseCase(IEquipmentCategoryRepository equipmentCategoryRepository)
    {
        EquipmentCategoryRepository = equipmentCategoryRepository;
    }

    public async Task<Guid> ExecuteAsync(CreateEquipmentCategoryDto createEquipmentCategoryDto, CancellationToken cancellationToken = default)
    {
        var equipmentCategoryToCreate = new EquipmentCategory(
            createEquipmentCategoryDto.Name,
            createEquipmentCategoryDto.Description
        );

        if (await EquipmentCategoryRepository.ExistsByNameAsync(equipmentCategoryToCreate.Name, cancellationToken))
        {
            throw new EquipmentCategoryAlreadyExistsException(equipmentCategoryToCreate.Name);
        }

        await EquipmentCategoryRepository.AddAsync(equipmentCategoryToCreate, cancellationToken);

        return equipmentCategoryToCreate.Id;
    }
}
