namespace MatosKC.Domain.Tests.Inspections;

using MatosKC.Domain.Entities.Inspection;

public sealed class InspectionMediaTests
{
    [Fact]
    public void Constructor_PreservesStorageMetadataAndOwnership()
    {
        Guid id = Guid.NewGuid();
        Guid inspectionId = Guid.NewGuid();
        Guid answerId = Guid.NewGuid();
        var media = new InspectionMedia(id, inspectionId, answerId, "photos/key", "photo.png", "image/png", 42);

        Assert.Equal(id, media.Id);
        Assert.Equal(inspectionId, media.InspectionId);
        Assert.Equal(answerId, media.AnswerId);
        Assert.Equal("photos/key", media.ObjectKey);
        Assert.Equal("photo.png", media.FileName);
        Assert.Equal("image/png", media.ContentType);
        Assert.Equal(42, media.ContentLength);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_RejectsEmptyIdentifiers(int emptyIndex)
    {
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        ids[emptyIndex] = Guid.Empty;

        Assert.Throws<ArgumentException>(() =>
            new InspectionMedia(ids[0], ids[1], ids[2], "key", "photo.png", "image/png", 1));
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(0, " ")]
    [InlineData(1, null)]
    [InlineData(1, "")]
    [InlineData(2, null)]
    [InlineData(2, " ")]
    public void Constructor_RejectsMissingStorageMetadata(int index, string? value)
    {
        string?[] values = ["key", "photo.png", "image/png"];
        values[index] = value;

        Assert.ThrowsAny<ArgumentException>(() => new InspectionMedia(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), values[0]!, values[1]!, values[2]!, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsEmptyContent(long length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InspectionMedia(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "key", "photo.png", "image/png", length));
    }
}
