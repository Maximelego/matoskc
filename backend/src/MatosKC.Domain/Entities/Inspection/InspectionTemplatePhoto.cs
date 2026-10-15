namespace MatosKC.Domain.Entities.Inspection;

public sealed class InspectionTemplatePhoto : BaseEntity
{
    public string ObjectKey { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long ContentLength { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public InspectionTemplatePhoto(
        Guid id, string objectKey, string fileName, string contentType,
        long contentLength, Guid createdByAccountId, DateTimeOffset createdAt
    ) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (contentType is not ("image/jpeg" or "image/png" or "image/webp")
            || contentLength is <= 0 or > 10 * 1024 * 1024 || createdByAccountId == Guid.Empty)
        {
            throw new ArgumentException("Invalid template illustration metadata.");
        }

        ObjectKey = objectKey;
        FileName = fileName;
        ContentType = contentType;
        ContentLength = contentLength;
        CreatedByAccountId = createdByAccountId;
        CreatedAt = createdAt;
    }
}
