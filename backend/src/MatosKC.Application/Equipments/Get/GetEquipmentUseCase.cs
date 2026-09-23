using MatosKC.Application.Equipments.Get.Exceptions;
using MatosKC.Application.Equipments.Mappings;
using MatosKC.Application.Equipments.Ports;


namespace MatosKC.Application.Equipments.Get;

public class GetEquipmentUseCase
{

    private readonly IEquipmentRepository _EquipmentRepository;

    public GetEquipmentUseCase(IEquipmentRepository equipmentRepository)
    {
        _EquipmentRepository = equipmentRepository;
    }
    public async Task<GetEquipmentDto> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _EquipmentRepository.RetrieveByIdAsync(id, cancellationToken);

        if (equipment is null)
        {
            throw new EquipmentNotFoundException(id);
        }

        return equipment.ToDto();
    }
}
