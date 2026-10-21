namespace MatosKC.Domain.Entities.Inspection;

public enum DefectReviewResult { Unchanged, Worsened, MaintenanceCheckRequired }

// Historical evidence is copied when the defect is presented, never queried live after validation.
public sealed class InspectionDefectReview : BaseEntity
{
    private List<Guid> _historicalPhotoIds = [];
    private List<Guid> _photoIds = [];
    public Guid InspectionId { get; private set; }
    public Guid DefectId { get; private set; }
    public string Description { get; private set; } = null!;
    public string HistoricalObservation { get; private set; } = null!;
    public DateTimeOffset ObservedAt { get; private set; }
    public DefectReviewResult? Result { get; private set; }
    public string? Observation { get; private set; }
    public IReadOnlyList<Guid> HistoricalPhotoIds => _historicalPhotoIds.AsReadOnly();
    public IReadOnlyList<Guid> PhotoIds => _photoIds.AsReadOnly();
    private InspectionDefectReview() { }
    internal InspectionDefectReview(Guid inspectionId, Guid defectId, string description,
        string observation, DateTimeOffset observedAt, IEnumerable<Guid> photos) : base(Guid.NewGuid())
    {
        if (inspectionId == Guid.Empty || defectId == Guid.Empty || observedAt == default)
            throw new ArgumentException("Inspection, defect and observation date are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(observation);
        var ids = photos.ToList();
        if (ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Count)
            throw new ArgumentException("Invalid historical photos.");
        InspectionId = inspectionId; DefectId = defectId; Description = description;
        HistoricalObservation = observation; ObservedAt = observedAt; _historicalPhotoIds = ids;
    }
    internal bool Refresh(string description, string observation, DateTimeOffset observedAt, IEnumerable<Guid> photos)
    {
        var ids = photos.ToList();
        if (Description == description && HistoricalObservation == observation && ObservedAt == observedAt
            && _historicalPhotoIds.SequenceEqual(ids)) return false;
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(observation);
        if (observedAt == default || ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Count)
            throw new ArgumentException("Invalid historical evidence.");
        Description = description; HistoricalObservation = observation; ObservedAt = observedAt;
        _historicalPhotoIds = ids; Result = null; Observation = null; _photoIds.Clear();
        return true;
    }

    internal void Update(DefectReviewResult result, string? observation)
    {
        if (!Enum.IsDefined(result)) throw new ArgumentException("Invalid review result.");
        if (result == DefectReviewResult.Unchanged && !string.IsNullOrWhiteSpace(observation))
            throw new ArgumentException("An unchanged defect does not require a new observation.");
        Result = result; Observation = string.IsNullOrWhiteSpace(observation) ? null : observation.Trim();
    }
    internal void AddPhoto(Guid id)
    {
        if (id == Guid.Empty || _photoIds.Contains(id)) throw new ArgumentException("Invalid photo.");
        _photoIds.Add(id);
    }
    internal void RemovePhoto(Guid id) => _photoIds.Remove(id);
    internal void EnsureComplete()
    {
        if (Result is null) throw new InvalidOperationException("Every known defect must be reviewed.");
        if (Result == DefectReviewResult.Worsened && (string.IsNullOrWhiteSpace(Observation) || _photoIds.Count == 0))
            throw new InvalidOperationException("A worsened defect requires a new observation and photo.");
    }
}
