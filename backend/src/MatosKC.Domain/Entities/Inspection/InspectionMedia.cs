namespace MatosKC.Domain.Entities.Inspection;

public sealed class InspectionMedia : BaseEntity
{
    public Guid InspectionId { get; private set; }

    public Guid AnswerId { get; private set; }

    public string ObjectKey { get; private set; }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public long ContentLength { get; private set; }

    public InspectionMedia(
        Guid id,
        Guid inspectionId,
        Guid answerId,
        string objectKey,
        string fileName,
        string contentType,
        long contentLength
    ) : base(id)
    {
        if (inspectionId == Guid.Empty || answerId == Guid.Empty)
        {
            throw new ArgumentException("Inspection and answer identifiers are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (contentLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contentLength));
        }

        InspectionId = inspectionId;
        AnswerId = answerId;
        ObjectKey = objectKey;
        FileName = fileName;
        ContentType = contentType;
        ContentLength = contentLength;
    }
}
