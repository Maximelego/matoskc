namespace MatosKC.Domain.Entities.Inspection;

using MatosKC.Domain.Entities;

public class InspectionTemplate : BaseEntity
{
    private InspectionTemplate()
    {
        _points = [];
    }

    private readonly List<InspectionTemplatePoint> _points;
    public IReadOnlyList<InspectionTemplatePoint> Points =>
        _points.AsReadOnly();

    public Guid EquipmentCategoryId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }


    public InspectionTemplate(
        Guid id,
        IEnumerable<InspectionTemplatePoint> points,
        Guid equipmentCategoryId
    ) : base(id)
    {
        ArgumentNullException.ThrowIfNull(points);

        if(equipmentCategoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Equipment category ID cannot be empty.",
                nameof(equipmentCategoryId)
            );
        }
        EquipmentCategoryId = equipmentCategoryId;

        _points = [.. points];

        if (_points.Count == 0)
        {
            throw new ArgumentException(
                "Template must contain at least one point.",
                nameof(points)
            );
        }

        if (_points.Any(point => point is null))
        {
            throw new ArgumentException(
                "Template points cannot contain null.",
                nameof(points)
            );
        }

        if (_points.Any(point => point.TemplateId != Id))
        {
            throw new ArgumentException(
                "Every point must belong to this template.",
                nameof(points)
            );
        }

        if (_points.Select(point => point.Id).Distinct().Count() != _points.Count)
        {
            throw new ArgumentException(
                "Template points must have unique identifiers.",
                nameof(points)
            );
        }

        CreatedAt = DateTimeOffset.UtcNow;
    }
}
