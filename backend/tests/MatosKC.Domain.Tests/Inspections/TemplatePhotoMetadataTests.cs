namespace MatosKC.Domain.Tests.Inspections;

using MatosKC.Domain.Entities.Inspection;

public sealed class TemplatePhotoMetadataTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Photo_RejectsInvalidMetadata(int scenario)
    {
        Assert.Throws<ArgumentException>(() => new InspectionTemplatePhoto(
            Guid.NewGuid(), "key", "guide.png", scenario == 0 ? "text/plain" : "image/png",
            scenario == 1 ? 0 : scenario == 2 ? 10485761 : 1,
            scenario == 3 ? Guid.Empty : Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void TemplatePoint_RequiresDescriptionDetail(string? detail)
    {
        Assert.Throws<ArgumentException>(() => new InspectionTemplatePoint(
            Guid.NewGuid(), Guid.NewGuid(), "Contrôle", detail!, null));
    }
}
