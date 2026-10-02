namespace MatosKC.Domain.Equipments;

public sealed class EquipmentPhoto
{
    public Guid Id { get; }
    public Guid EquipmentId { get; }
    public string ObjectKey { get; }
    public string OriginalFileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long ContentLength { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public EquipmentPhoto(
        Guid id,
        Guid equipmentId,
        string objectKey,
        string originalFileName,
        string contentType,
        long contentLength,
        DateTimeOffset createdAtUtc
    )
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Photo identifier cannot be empty.", nameof(id));
        }

        if (equipmentId == Guid.Empty)
        {
            throw new ArgumentException("Equipment identifier cannot be empty.", nameof(equipmentId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        Id = id;
        EquipmentId = equipmentId;
        ObjectKey = objectKey.Trim();
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;

        ReplaceMetadata(originalFileName, contentType, contentLength, createdAtUtc);
    }

    public void ReplaceMetadata(
        string originalFileName,
        string contentType,
        long contentLength,
        DateTimeOffset updatedAtUtc
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (contentLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contentLength),
                "Photo content length must be greater than zero."
            );
        }

        OriginalFileName = Path.GetFileName(originalFileName.Trim());
        ContentType = contentType.Trim().ToLowerInvariant();
        ContentLength = contentLength;
        UpdatedAtUtc = updatedAtUtc;
    }
}
