namespace MatosKC.Domain.Entities.Maintenance;

public sealed class DefectOccurrence : BaseEntity
{
    private List<Guid> _photoIds = [];

    public Guid DefectId { get; private set; }
    public Guid InspectionId { get; private set; }
    public Guid? InspectionAnswerId { get; private set; }
    public Guid? InspectionFindingId { get; private set; }
    public Guid? InspectionDefectReviewId { get; private set; }
    public string Description { get; private set; } = null!;
    public string Observation { get; private set; } = null!;
    public DateTimeOffset ObservedAt { get; private set; }
    public IReadOnlyList<Guid> PhotoIds => _photoIds.AsReadOnly();

    private DefectOccurrence() { }

    public DefectOccurrence(Guid id, Guid defectId, Guid inspectionId, Guid inspectionAnswerId,
        string description, string observation, DateTimeOffset observedAt, IEnumerable<Guid> photoIds) : base(id)
    {
        if (defectId == Guid.Empty || inspectionId == Guid.Empty || inspectionAnswerId == Guid.Empty)
        {
            throw new ArgumentException("Defect and inspection identifiers are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(observation);
        ArgumentNullException.ThrowIfNull(photoIds);
        var photos = photoIds.ToList();

        if (observedAt == default || photos.Count == 0 || photos.Contains(Guid.Empty)
            || photos.Distinct().Count() != photos.Count)
        {
            throw new ArgumentException("A date and distinct nonempty photo identifiers are required.");
        }

        DefectId = defectId;
        InspectionId = inspectionId;
        InspectionAnswerId = inspectionAnswerId;
        Description = description;
        Observation = observation;
        ObservedAt = observedAt.ToUniversalTime();
        _photoIds = photos;
    }

    public static DefectOccurrence FromEvidence(Guid id, Guid defectId, Guid inspectionId,
        Guid evidenceId, bool isReview, string description, string observation, DateTimeOffset at, IEnumerable<Guid> photos)
    {
        var value = new DefectOccurrence(id, defectId, inspectionId, evidenceId, description, observation, at, photos);
        value.InspectionAnswerId = null;
        if (isReview) value.InspectionDefectReviewId = evidenceId;
        else value.InspectionFindingId = evidenceId;
        return value;
    }
}
