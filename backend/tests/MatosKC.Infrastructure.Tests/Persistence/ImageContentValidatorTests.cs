namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Infrastructure.Storage;
using SkiaSharp;

public sealed class ImageContentValidatorTests
{
    [Theory]
    [InlineData(SKEncodedImageFormat.Png, "image/png")]
    [InlineData(SKEncodedImageFormat.Jpeg, "image/jpeg")]
    [InlineData(SKEncodedImageFormat.Webp, "image/webp")]
    public void Validator_AcceptsRealImagesAndRejectsForgedTypesAndTruncation(SKEncodedImageFormat format, string mime)
    {
        using var bitmap = new SKBitmap(40, 30);
        bitmap.Erase(SKColors.Orange);
        using var encoded = bitmap.Encode(format, 90);
        byte[] bytes = encoded.ToArray();
        var validator = new ImageContentValidator();
        validator.Validate(bytes, mime);
        Assert.Throws<ArgumentException>(() => validator.Validate(bytes, mime == "image/png" ? "image/jpeg" : "image/png"));
        Assert.Throws<ArgumentException>(() => validator.Validate(bytes[..(bytes.Length / 2)], mime));
    }

    [Fact]
    public void Validator_RejectsAnimatedWebp()
    {
        byte[] animated = Convert.FromBase64String("UklGRuQAAABXRUJQVlA4WAoAAAACAAAAHQAAHQAAQU5JTQYAAAAAAAAAAABBTk1GXAAAAAAAAAAAAB0AAB0AAMgAAAJWUDggRAAAALADAJ0BKh4AHgA+bTSWR6QjIiEoCACADYllAMkCgH4AAtaQY7UAAP7wm0P/yC5YXXI1//ID/kB/yA//kB/+m9mp84AAQU5NRlQAAAAAAAAAAAAdAAAdAADIAAAAVlA4IDwAAADUAgCdASoeAB4APm00lkeCgIAAANiWUAdgAExQ4dowAP75Hdv//kB//kB//kB//ID//iF3shz/9P4AAAA=");
        Assert.Throws<ArgumentException>(() => new ImageContentValidator().Validate(animated, "image/webp"));
    }

    [Fact]
    public void Validator_RejectsNonImagesAndOversizedPayloads()
    {
        var validator = new ImageContentValidator();
        Assert.Throws<ArgumentException>(() => validator.Validate([1, 2, 3], "image/png"));
        Assert.Throws<ArgumentException>(() => validator.Validate(new byte[10 * 1024 * 1024 + 1], "image/png"));
    }

    [Fact]
    public void Validator_RejectsExcessiveDimensionsBeforeDecoding()
    {
        using var bitmap = new SKBitmap(6000, 4500);
        bitmap.Erase(SKColors.White);
        using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        Assert.Throws<ArgumentException>(() => new ImageContentValidator().Validate(encoded.ToArray(), "image/png"));
    }
}
