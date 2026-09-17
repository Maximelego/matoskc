using System.Runtime.CompilerServices;

namespace MatosKC.Domain.Equipments;

public class EquipmentCategory
{
    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }

    public EquipmentCategory(Guid id, string name, string? description)
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
        Name = name;
        Description = description;
    }

    public EquipmentCategory(string name, string? description) :this(Guid.NewGuid(), name, description)
    {
    }
}