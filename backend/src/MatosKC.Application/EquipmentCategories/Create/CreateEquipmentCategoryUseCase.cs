namespace MatosKC.Application.EquipmentCategories.Create;

using MatosKC.Application.EquipmentCategories.Create.Exceptions;
using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Domain.Equipments;

public class CreateEquipmentCategoryUseCase
{
    private readonly IEquipmentCategoryRepository EquipmentCategoryRepository;
    private readonly MatosKC.Application.Agencies.AgencyScope? Scope;
    private readonly MatosKC.Application.Agencies.Ports.IAgencyRepository? Agencies;

    public CreateEquipmentCategoryUseCase(IEquipmentCategoryRepository equipmentCategoryRepository,
        MatosKC.Application.Agencies.AgencyScope? scope = null,
        MatosKC.Application.Agencies.Ports.IAgencyRepository? agencies = null)
    {
        EquipmentCategoryRepository = equipmentCategoryRepository;
        Scope = scope;
        Agencies = agencies;
    }

    public async Task<Guid> ExecuteAsync(CreateEquipmentCategoryDto createEquipmentCategoryDto, CancellationToken cancellationToken = default)
    {
        var equipmentCategoryToCreate = new EquipmentCategory(
            createEquipmentCategoryDto.Name,
            createEquipmentCategoryDto.Description
        );

        var owner = Scope?.ResolveOwner(createEquipmentCategoryDto.AgencyId) ?? createEquipmentCategoryDto.AgencyId;
        if (owner.HasValue && Agencies is not null && !await Agencies.ExistsByIdAsync(owner.Value, cancellationToken))
            throw new MatosKC.Application.Inspections.InspectionOperationException(404, "Agency not found.");
        if (owner.HasValue) equipmentCategoryToCreate.AssignAgency(owner.Value);
        if (await EquipmentCategoryRepository.ExistsByNameAsync(equipmentCategoryToCreate.Name, cancellationToken, owner))
        {
            throw new EquipmentCategoryAlreadyExistsException(equipmentCategoryToCreate.Name);
        }

        await EquipmentCategoryRepository.AddAsync(equipmentCategoryToCreate, cancellationToken);

        return equipmentCategoryToCreate.Id;
    }
}
