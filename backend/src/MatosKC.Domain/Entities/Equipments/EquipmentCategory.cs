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
        Name = name.Trim();

        if (description != null)
        {
            description = description.Trim();
            Description = description == "" ? null : description;  
        }
    }

    public EquipmentCategory(string name, string? description) :this(Guid.NewGuid(), name, description)
    {
    }


}