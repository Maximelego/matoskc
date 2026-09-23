namespace MatosKC.Application.Equipments.List;

using MatosKC.Application.Equipments.Mappings;
using MatosKC.Application.Equipments.Ports;

public class ListEquipmentUseCase
{
    private readonly IEquipmentRepository _equipmentRepository;

    public ListEquipmentUseCase(IEquipmentRepository equipmentRepository)
    {
        _equipmentRepository = equipmentRepository;
    }

    public async Task<ListEquipmentResult> ExecuteAsync(ListEquipmentsQuery query, CancellationToken cancellationToken)
    {
        var equipments = await _equipmentRepository.ListEquipmentsAsync(query.CategoryId, query.Status, query.Search, cancellationToken);

        return new ListEquipmentResult(
            equipments.Select(equipment => equipment.ToDto())
                      .ToList()
        );
    }
}
