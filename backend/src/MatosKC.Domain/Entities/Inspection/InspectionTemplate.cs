namespace MatosKC.Domain.Entities.Inspection;

public class InspectionTemplate : BaseEntity
{
    public List<InspectionTemplatePoint> Points { get; private set; }

    public InspectionTemplate(Guid id, List<InspectionTemplatePoint> points): base(id)
    {
        Points = points;
    }

    public void AddPoints(List<InspectionTemplatePoint> points)
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points), "Points cannot be null.");
        if (points.Count == 0)
            throw new ArgumentException("Points list cannot be empty.", nameof(points));

        Points.AddRange(points);
    }
}
