namespace MatosKC.Domain.Entities.Inspection;

public class InspectionAnswer: BaseEntity
{
    public Guid InspectionId { get; private set; }
    public Guid TemplatePointId { get; private set; }
    public InspectionResult Result { get; private set; }
    public string? Comment { get; private set; }
    public List<Guid> PhotosIds { get; private set; }

    public InspectionAnswer(Guid id, Guid inspectionId, Guid templatePointId, InspectionResult result, string? comment, List<Guid> photosIds) :
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
        PhotosIds = photosIds;
    }

    public void AddPhoto(Guid photoId) {
        if (photoId == Guid.Empty)
            throw new ArgumentException("Photo ID cannot be empty.", nameof(photoId));

        PhotosIds.Add(photoId);
    }

    public void RemovePhoto(Guid photoId) {
        if (photoId == Guid.Empty)
            throw new ArgumentException("Photo ID cannot be empty.", nameof(photoId));

        PhotosIds.Remove(photoId);
    }
}
