namespace MatosKC.Domain.Equipments;

public class Equipment
{
    public Guid Id { get; }
    public string Name { get; }
    public Guid CategoryId { get; }
    public string SerialNumber { get; }
    public EquipmentStatus Status { get; private set; }

    public Equipment(Guid id, string name, Guid categoryId, string serialNumber)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Equipment Identifier cannot be Empty",
                nameof(id)
            );
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Equipment's Category Identifier cannot be Empty",
                nameof(categoryId)
            );
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);

        Id = id;
        Name = name.Trim();
        CategoryId = categoryId;
        SerialNumber = serialNumber.Trim();
        Status = EquipmentStatus.Available;
    }

    public Equipment(string name, Guid categoryId, string serialNumber) : this(Guid.NewGuid(), name, categoryId, serialNumber)
    { }

    public bool CanTransitionTo(EquipmentStatus newStatus)
    {
        return (Status, newStatus) switch
        {
            (EquipmentStatus.Available, EquipmentStatus.Borrowed) => true,
            (EquipmentStatus.Available, EquipmentStatus.Decommissioned) => true,

            (EquipmentStatus.Borrowed, EquipmentStatus.Available) => true,
            (EquipmentStatus.Borrowed, EquipmentStatus.ToBeDecided) => true,

            (EquipmentStatus.ToBeDecided, EquipmentStatus.Available) => true,
            (EquipmentStatus.ToBeDecided, EquipmentStatus.Unavailable) => true,

            (EquipmentStatus.Unavailable, EquipmentStatus.Available) => true,
            (EquipmentStatus.Unavailable, EquipmentStatus.Maintenance) => true,
            (EquipmentStatus.Unavailable, EquipmentStatus.Decommissioned) => true,

            (EquipmentStatus.Maintenance, EquipmentStatus.Available) => true,
            (EquipmentStatus.Maintenance, EquipmentStatus.Unavailable) => true,
            (EquipmentStatus.Maintenance, EquipmentStatus.Decommissioned) => true,

            // Any other case
            _ => false,
        };
    }

    public void ChangeStatus(EquipmentStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition equipment from {Status} to {newStatus}.");
        }
        Status = newStatus;
    }

}
