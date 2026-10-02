namespace MatosKC.Application.EquipmentPhotos;

internal static class EquipmentPhotoFileValidator
{
    public const long MaximumContentLength = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    public static void Validate(string fileName, string contentType, long contentLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (contentLength <= 0)
        {
            throw new ArgumentException("The photo is empty.", nameof(contentLength));
        }

        if (contentLength > MaximumContentLength)
        {
            throw new ArgumentException("The photo must not exceed 10 MB.", nameof(contentLength));
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException(
                "Only JPEG, PNG and WebP photos are accepted.",
                nameof(contentType)
            );
        }
    }
}
