namespace MatosKC.Domain.Equipments;

using MatosKC.Domain.Entities;

public class Equipment: BaseEntity
{
    public string Name { get; private set; }
    public Guid CategoryId { get; private set; }
    public string SerialNumber { get; }
    public EquipmentStatus Status { get; private set; }

    public Equipment(Guid id, string name, Guid categoryId, string serialNumber)
    : base(id)
    {
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
    {
    }

    public void UpdateDetails(string name, Guid categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category identifier cannot be empty.", nameof(categoryId));
        }

        Name = name.Trim();
        CategoryId = categoryId;
    }

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
