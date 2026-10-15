namespace MatosKC.Infrastructure.Reports;

using MatosKC.Application.Inspections;
using MatosKC.Application.Inspections.Ports;
using SkiaSharp;
using MatosKC.Application.Files.Ports;

public sealed class InspectionSignatureValidator(IImageContentValidator images) : IInspectionSignatureValidator
{
    public void Validate(byte[] png)
    {
        if (png.Length is 0 or > 1024 * 1024)
        {
            throw Invalid();
        }

        using var data = SKData.CreateCopy(png);
        using var codec = SKCodec.Create(data);

        if (codec is null || codec.EncodedFormat != SKEncodedImageFormat.Png
            || codec.Info.Width is < 20 or > 2048 || codec.Info.Height is < 20 or > 2048)
        {
            throw Invalid();
        }

        try
        {
            images.Validate(png, "image/png");
        }
        catch (ArgumentException)
        {
            throw Invalid();
        }

        using var bitmap = SKBitmap.Decode(png);

        if (bitmap is null || bitmap.Pixels.Count(pixel => pixel.Alpha > 32
                && (pixel.Red < 230 || pixel.Green < 230 || pixel.Blue < 230)) < 10)
        {
            throw Invalid();
        }
    }

    private static InspectionOperationException Invalid() =>
        new(400, "Use a non-empty PNG signature, 20-2048 pixels per side, at most 1 MB.");
}
