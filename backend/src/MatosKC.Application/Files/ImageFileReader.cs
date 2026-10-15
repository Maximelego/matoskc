namespace MatosKC.Application.Files;

using MatosKC.Application.Files.Ports;

public static class ImageFileReader
{
    public const int MaximumLength = 10 * 1024 * 1024;

    public static async Task<byte[]> ReadValidatedAsync(
        Stream source,
        long declaredLength,
        string contentType,
        IImageContentValidator validator,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(source);

        if (declaredLength is <= 0 or > MaximumLength)
        {
            throw new ArgumentException("Use an image of at most 10 MB.", nameof(declaredLength));
        }

        using var target = new MemoryStream();
        byte[] buffer = new byte[81920];
        int read;

        while ((read = await source.ReadAsync(buffer.AsMemory(), ct)) > 0)
        {
            if (target.Length + read > MaximumLength || target.Length + read > declaredLength)
            {
                throw new ArgumentException("Image length does not match the declared size.", nameof(source));
            }

            await target.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        if (target.Length != declaredLength)
        {
            throw new ArgumentException("Image length does not match the declared size.", nameof(source));
        }

        byte[] bytes = target.ToArray();
        validator.Validate(bytes, contentType);

        return bytes;
    }
}
