using MatosKC.Application.Equipments.Ports;
using MatosKC.Domain.Equipments;

namespace MatosKC.Application.Tests.Fakes;

internal sealed class FakeEquipmentRepository : IEquipmentRepository
{
    private readonly Dictionary<Guid, Equipment> _equipments = [];

    public bool SerialNumberAlreadyExists { get; set; }

    public List<Equipment> AddedEquipments { get; } = [];

    public string? LastCheckedSerialNumber { get; private set; }

    public CancellationToken ExistsCancellationToken { get; private set; }

    public CancellationToken AddCancellationToken { get; private set; }

    public Guid? LastRequestedId { get; private set; }

    public CancellationToken RetrieveCancellationToken { get; private set; }

    public Guid? LastCategoryIdFilter { get; private set; }

    public EquipmentStatus? LastStatusFilter { get; private set; }

    public string? LastSearchFilter { get; private set; }

    public CancellationToken ListCancellationToken { get; private set; }

    public void Seed(Equipment equipment)
    {
        _equipments.Add(equipment.Id, equipment);
        AddedEquipments.Add(equipment);
    }

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
        _equipments[equipment.Id] = equipment;
        AddedEquipments.Add(equipment);
        AddCancellationToken = cancellationToken;

        return Task.CompletedTask;
    }

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_equipments.ContainsKey(id));
    }

    public Task<Equipment?> RetrieveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        LastRequestedId = id;
        RetrieveCancellationToken = cancellationToken;
        _equipments.TryGetValue(id, out Equipment? equipment);

        return Task.FromResult(equipment);
    }

    public Task<List<Equipment>> ListEquipmentsAsync(Guid? categoryId, EquipmentStatus? status, string? search, CancellationToken cancellationToken)
    {
        LastCategoryIdFilter = categoryId;
        LastStatusFilter = status;
        LastSearchFilter = search;
        ListCancellationToken = cancellationToken;

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
