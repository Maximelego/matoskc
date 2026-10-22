namespace MatosKC.Domain.Equipments;

using MatosKC.Domain.Entities;

public class EquipmentCategory : BaseEntity, MatosKC.Domain.Entities.Agencies.IAgencyOwned
{
    public Guid AgencyId { get; private set; }

    public void AssignAgency(Guid agencyId)
    {
        if (agencyId == Guid.Empty) throw new ArgumentException("Agency is required.", nameof(agencyId));
        if (AgencyId != Guid.Empty && AgencyId != agencyId)
            throw new InvalidOperationException("Agency ownership cannot be transferred.");
        AgencyId = agencyId;
    }

    public string Name { get; private set; }
    public string? Description { get; private set; }

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

    public void UpdateDetails(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public EquipmentCategory(string name, string? description) : this(Guid.NewGuid(), name, description)
    {
    }


}
