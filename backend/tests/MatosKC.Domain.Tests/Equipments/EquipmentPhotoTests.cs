namespace MatosKC.Domain.Tests.Equipments;

using MatosKC.Domain.Equipments;

public sealed class EquipmentPhotoTests
{
    [Fact]
    public void Constructor_ShouldNormalizeFileMetadata()
    {
        Guid equipmentId = Guid.NewGuid();
        Guid photoId = Guid.NewGuid();

        var photo = new EquipmentPhoto(
            photoId,
            equipmentId,
            $"equipments/{equipmentId}/photos/{photoId}",
            " ../machine.PNG ",
            " IMAGE/PNG ",
            42,
            DateTimeOffset.UtcNow
        );

        Assert.Equal("machine.PNG", photo.OriginalFileName);
        Assert.Equal("image/png", photo.ContentType);
        Assert.Equal(42, photo.ContentLength);
    }

    [Fact]
    public void Constructor_WithEmptyEquipmentId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new EquipmentPhoto(
            Guid.NewGuid(),
            Guid.Empty,
            "object-key",
            "photo.png",
            "image/png",
            1,
            DateTimeOffset.UtcNow
        ));
    }

    [Fact]
    public void ReplaceMetadata_WithEmptyContent_ShouldThrow()
    {
        var photo = new EquipmentPhoto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "object-key",
            "photo.png",
            "image/png",
            1,
            DateTimeOffset.UtcNow
        );

        Assert.Throws<ArgumentOutOfRangeException>(() => photo.ReplaceMetadata(
            "photo.png",
            "image/png",
            0,
            DateTimeOffset.UtcNow
        ));
    }
}
