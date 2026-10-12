namespace MatosKC.Domain.Entities.Inspection;

public class InspectionTemplatePoint: BaseEntity
{
    public Guid TemplateId { get; private set; }
    public string Description { get; private set; }
    public Guid? PhotoId { get; private set; }

    public InspectionTemplatePoint(Guid id, Guid templateId, string description, Guid? photoId) : base(id)
    {
        if (templateId == Guid.Empty)
            throw new ArgumentException("Template ID cannot be empty.", nameof(templateId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or whitespace.", nameof(description));

        if (photoId.HasValue && photoId.Value == Guid.Empty)
            throw new ArgumentException("Photo ID cannot be empty if provided.", nameof(photoId));

        TemplateId = templateId;
        Description = description;
        PhotoId = photoId;
    }
}
