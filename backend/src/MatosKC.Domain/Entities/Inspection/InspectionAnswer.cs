namespace MatosKC.Domain.Entities.Inspection;

public class InspectionAnswer: BaseEntity
{
    public Guid InspectionId { get; private set; }
    public Guid TemplatePointId { get; private set; }
    public InspectionResult Result { get; private set; }
    public string? Comment { get; private set; }

    private readonly List<Guid> _photos = [];
    public IReadOnlyList<Guid> PhotosIds =>
        _photos.AsReadOnly();

    public InspectionAnswer(Guid id, Guid inspectionId, Guid templatePointId, InspectionResult result, string? comment) :
    base(id) {
        if (inspectionId == Guid.Empty)
            throw new ArgumentException("Inspection ID cannot be empty.", nameof(inspectionId));

        if (templatePointId == Guid.Empty)
            throw new ArgumentException("Template Point ID cannot be empty.", nameof(templatePointId));

        if (!Enum.IsDefined(result))
            throw new ArgumentException("Invalid inspection result.", nameof(result));

        if (result == InspectionResult.NonCompliant && string.IsNullOrWhiteSpace(comment))
            throw new ArgumentException("Comment is required for non-compliant results.", nameof(comment));

        InspectionId = inspectionId;
        TemplatePointId = templatePointId;
        Result = result;
        Comment = comment;
    }

    internal void AddPhoto(Guid photoId) {
        if (photoId == Guid.Empty)
            throw new ArgumentException("Photo ID cannot be empty.", nameof(photoId));

        _photos.Add(photoId);
    }

    internal void RemovePhoto(Guid photoId) {
        if (photoId == Guid.Empty)
            throw new ArgumentException("Photo ID cannot be empty.", nameof(photoId));

        _photos.Remove(photoId);
    }
}
