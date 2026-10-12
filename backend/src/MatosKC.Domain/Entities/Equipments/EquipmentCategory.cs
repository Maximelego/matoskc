namespace MatosKC.Domain.Equipments;

using MatosKC.Domain.Entities;

public class EquipmentCategory : BaseEntity
{
    public string Name { get; }
    public string? Description { get; }

    public EquipmentCategory(Guid id, string name, string? description)
    : base(id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Category Identifier cannot be Empty",
                nameof(id)
            );
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    public EquipmentCategory(string name, string? description) : this(Guid.NewGuid(), name, description)
    {
    }


}
