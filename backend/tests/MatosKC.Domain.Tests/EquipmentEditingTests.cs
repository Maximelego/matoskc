namespace MatosKC.Domain.Tests;

using MatosKC.Domain.Equipments;

public sealed class EquipmentEditingTests
{
    [Fact]
    public void DetailsChange_LeavesSerialAndStatusIntact()
    {
        var equipment = new Equipment("Old", Guid.NewGuid(), "FIXED-SERIAL");
        equipment.ChangeStatus(EquipmentStatus.Borrowed);
        var categoryId = Guid.NewGuid();

        equipment.UpdateDetails("  Nouveau nom  ", categoryId);

        Assert.Equal("Nouveau nom", equipment.Name);
        Assert.Equal(categoryId, equipment.CategoryId);
        Assert.Equal("FIXED-SERIAL", equipment.SerialNumber);
        Assert.Equal(EquipmentStatus.Borrowed, equipment.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void InvalidName_DoesNotPartiallyChangeEquipment(string name)
    {
        var equipment = new Equipment("Original", Guid.NewGuid(), "SERIAL");
        var categoryId = equipment.CategoryId;

        Assert.Throws<ArgumentException>(() => equipment.UpdateDetails(name, Guid.NewGuid()));
        Assert.Equal("Original", equipment.Name);
        Assert.Equal(categoryId, equipment.CategoryId);
    }

    [Fact]
    public void InvalidCategory_DoesNotPartiallyChangeEquipment()
    {
        var equipment = new Equipment("Original", Guid.NewGuid(), "SERIAL");

        Assert.Throws<ArgumentException>(() => equipment.UpdateDetails("Other", Guid.Empty));
        Assert.Equal("Original", equipment.Name);
    }
}
