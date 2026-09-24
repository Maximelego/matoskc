using MatosKC.Application.Equipments.Ports;
using MatosKC.Domain.Equipments;

namespace MatosKC.Application.Tests.Fakes;

internal sealed class FakeEquipmentRepository : IEquipmentRepository
{
    public bool SerialNumberAlreadyExists { get; set; }

    public List<Equipment> AddedEquipments { get; } = [];

    public string? LastCheckedSerialNumber { get; private set; }

    public CancellationToken ExistsCancellationToken { get; private set; }

    public CancellationToken AddCancellationToken { get; private set; }

    public Task<bool> ExistsBySerialNumberAsync(
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        LastCheckedSerialNumber = serialNumber;
        ExistsCancellationToken = cancellationToken;

        return Task.FromResult(SerialNumberAlreadyExists);
    }

    public Task AddAsync(
        Equipment equipment,
        CancellationToken cancellationToken = default)
    {
        AddedEquipments.Add(equipment);
        AddCancellationToken = cancellationToken;

        return Task.CompletedTask;
    }

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Equipment?> RetrieveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(AddedEquipments.FirstOrDefault(equipment => equipment.Id == id));
    }

    public Task<List<Equipment>> ListEquipmentsAsync(Guid? categoryId, EquipmentStatus? status, string? search, CancellationToken cancellationToken)
    {
        var filteredEquipments = AddedEquipments.AsEnumerable();

        if (categoryId.HasValue)
        {
            filteredEquipments = filteredEquipments.Where(equipment => equipment.CategoryId == categoryId.Value);
        }

        if (status.HasValue)
        {
            filteredEquipments = filteredEquipments.Where(equipment => equipment.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            filteredEquipments = filteredEquipments.Where(equipment =>
                equipment.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                equipment.SerialNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(filteredEquipments.ToList());
    }
}
