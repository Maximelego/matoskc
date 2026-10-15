namespace MatosKC.Domain.Entities.Inspection;

public class InspectionAnswer : BaseEntity
{
    public Guid InspectionId { get; private set; }

    public Guid TemplatePointId { get; private set; }

    public InspectionResult Result { get; private set; }

    public Guid? ExistingDefectId { get; private set; }

    public string? Comment { get; private set; }

    private readonly List<Guid> _photos = [];

    public IReadOnlyList<Guid> PhotosIds => _photos.AsReadOnly();

    public InspectionAnswer(
        Guid id,
        Guid inspectionId,
        Guid templatePointId,
        InspectionResult result,
        string? comment
    ) : base(id)
    {
        if (inspectionId == Guid.Empty)
        {
            throw new ArgumentException("Inspection ID cannot be empty.", nameof(inspectionId));
        }

        if (templatePointId == Guid.Empty)
        {
            throw new ArgumentException("Template point ID cannot be empty.", nameof(templatePointId));
        }

        InspectionId = inspectionId;
        TemplatePointId = templatePointId;

        UpdateResult(result, comment);
    }

    internal void UpdateResult(InspectionResult result, string? comment)
    {
        if (!Enum.IsDefined(result))
        {
            throw new ArgumentException("Invalid inspection result.", nameof(result));
        }

        if (result == InspectionResult.Compliant && !string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("A compliant answer cannot have an observation.", nameof(comment));
        }

        if (result == InspectionResult.Compliant)
        {
            ExistingDefectId = null;
        }

        Result = result;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }

    internal void LinkExistingDefect(Guid? defectId)
    {
        if (defectId == Guid.Empty || (defectId.HasValue && Result != InspectionResult.NonCompliant))
        {
            throw new ArgumentException("Only a noncompliant answer can reference an existing defect.");
        }

        ExistingDefectId = defectId;
    }

    internal void AddPhoto(Guid photoId)
    {
        if (photoId == Guid.Empty)
        {
            throw new ArgumentException("Photo ID cannot be empty.", nameof(photoId));
        }

        if (_photos.Contains(photoId))
        {
            throw new InvalidOperationException("Photo is already attached to this answer.");
        }

        _photos.Add(photoId);
    }

    internal void RemovePhoto(Guid photoId)
    {
        if (photoId == Guid.Empty)
        {
            throw new ArgumentException("Photo ID cannot be empty.", nameof(photoId));
        }

        _photos.Remove(photoId);
    }
}
