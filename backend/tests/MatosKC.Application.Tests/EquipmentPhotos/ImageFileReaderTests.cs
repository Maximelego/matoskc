namespace MatosKC.Application.Tests.EquipmentPhotos;

using MatosKC.Application.Files;
using MatosKC.Application.Tests.Fakes;

public sealed class ImageFileReaderTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(10485761)]
    public async Task Reader_RejectsInvalidOrFalseDeclaredLengthBeforeDecoding(int declared)
    {
        var validator = new FakeImageContentValidator();
        await Assert.ThrowsAsync<ArgumentException>(() => ImageFileReader.ReadValidatedAsync(
            new MemoryStream([1, 2, 3]), declared, "image/png", validator));
        Assert.Equal(0, validator.Calls);
    }

    [Fact]
    public async Task Reader_HonorsCancellation()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ImageFileReader.ReadValidatedAsync(
            new MemoryStream([1]), 1, "image/png", new FakeImageContentValidator(), cancelled.Token));
    }
}
