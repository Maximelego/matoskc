namespace MatosKC.Domain.Entities.Inspection;

public sealed class InspectionFinding : BaseEntity
{
    private List<Guid> _photoIds = [];
    public Guid InspectionId { get; private set; }
    public Guid TemplatePointId { get; private set; }
    public bool IsRemoved { get; private set; }
    public string Observation { get; private set; } = null!;
    public IReadOnlyList<Guid> PhotoIds => _photoIds.AsReadOnly();
    private InspectionFinding() { }
    internal InspectionFinding(Guid id, Guid inspectionId, Guid pointId, string observation) : base(id)
    {
        if (inspectionId == Guid.Empty || pointId == Guid.Empty) throw new ArgumentException("Invalid finding identifiers.");
        InspectionId = inspectionId; TemplatePointId = pointId; Update(observation);
    }
    internal void Update(string observation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(observation); Observation = observation.Trim();
    }
    internal void Remove() => IsRemoved = true;

    internal void AddPhoto(Guid id)
    {
        if (id == Guid.Empty || _photoIds.Contains(id)) throw new ArgumentException("Invalid photo.");
        _photoIds.Add(id);
    }
    internal void RemovePhoto(Guid id) => _photoIds.Remove(id);
}
