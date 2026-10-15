namespace MatosKC.Infrastructure.Storage;

using MatosKC.Application.Files;
using MatosKC.Application.Files.Ports;
using SkiaSharp;

public sealed class ImageContentValidator : IImageContentValidator
{
    public const long MaximumPixels = 25_000_000;

    public void Validate(byte[] content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (content.Length is 0 or > ImageFileReader.MaximumLength)
        {
            throw Invalid();
        }

        SKEncodedImageFormat expected = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => SKEncodedImageFormat.Jpeg,
            "image/png" => SKEncodedImageFormat.Png,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => throw Invalid()
        };
        using var data = SKData.CreateCopy(content);
        using var codec = SKCodec.Create(data);

        if (codec is null || codec.EncodedFormat != expected || codec.FrameCount > 1
            || codec.Info.Width <= 0 || codec.Info.Height <= 0
            || (long)codec.Info.Width * codec.Info.Height > MaximumPixels)
        {
            throw Invalid();
        }

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);

        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
        {
            throw Invalid();
        }
    }

    private static ArgumentException Invalid() => new(
        "Use a complete, non-animated JPEG, PNG or WebP image matching its declared type, at most 10 MB and 25 million pixels.");
}
