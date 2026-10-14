namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Application.Inspections;
using MatosKC.Infrastructure.Reports;
using SkiaSharp;

public sealed class InspectionSignatureImageTests
{
    [Fact]
    public void Validator_RejectsBlankForgedAndOversizedSignatures()
    {
        var validator = new InspectionSignatureValidator();
        Assert.Throws<InspectionOperationException>(() => validator.Validate([1, 2, 3]));
        Assert.Throws<InspectionOperationException>(() => validator.Validate(new byte[1024 * 1024 + 1]));
        using var bitmap = new SKBitmap(100, 40);
        bitmap.Erase(SKColors.White);
        using var blank = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        Assert.Throws<InspectionOperationException>(() => validator.Validate(blank.ToArray()));
        using var canvas = new SKCanvas(bitmap);
        using var pen = new SKPaint { Color = SKColors.Black, StrokeWidth = 3 };
        canvas.DrawLine(10, 30, 80, 10, pen);
        using var signed = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        validator.Validate(signed.ToArray());
    }
}
