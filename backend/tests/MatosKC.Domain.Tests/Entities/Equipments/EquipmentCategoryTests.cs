using MatosKC.Domain.Equipments;

namespace MatosKC.Domain.Tests.Equipments;

public class EquipmentCategoryTests
{

    [Fact]
    public void Constructor_WithoutId_ShouldGenerateNonEmptyId()
    {
        // Arrange et Act
        var equipmentCategory = new EquipmentCategory(
            "Fenwick",
            "This is an optional description");

        // Assert
        Assert.NotEqual(Guid.Empty, equipmentCategory.Id);
    }

    [Fact]
    public void Constructor_WithId_ShouldPreserveProvidedId()
    {
        var id = Guid.NewGuid();
        
        var equipmentCategory = new EquipmentCategory(
            id,
            "Fenwick",
            "This is an optional description");

        // Assert
        Assert.Equal(id, equipmentCategory.Id);
    }

    [Fact]
    public void Constructor_WithEmptyId_ShouldThrowArgumentException()
    {
        var id = Guid.Empty;
        
        var action = () => new EquipmentCategory(
            id,
            "Fenwick",
            "This is an optional description");

        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(
        string? invalidName
    )
    {        
        var action = () => new EquipmentCategory(
            Guid.NewGuid(),
            invalidName!,
            "This is an optional description");

        Assert.ThrowsAny<ArgumentException>(action);
    }

    [Theory]
    [InlineData("  Fenwick  ")]
    [InlineData("Fenwick")]
    public void Constructor_ValidName_ShouldTrimName(
        string name
    )
    {        
        var equipmentCategory = new EquipmentCategory(
            Guid.NewGuid(),
            name,
            "This is an optional description");

        Assert.Equal(name.Trim(), equipmentCategory.Name);
    }

    [Fact]
    public void Constructor_WithNullDescription_ShouldKeepDescription()
    {        
        var equipmentCategory = new EquipmentCategory(
            Guid.NewGuid(),
            "Fenwick",
            null);

        Assert.Null(equipmentCategory.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyDescription_ShouldSetDescriptionAsNull(
        string? description
    )
    {        
        var equipmentCategory = new EquipmentCategory(
            Guid.NewGuid(),
            "Fenwick",
            description);

        Assert.Null(equipmentCategory.Description);
    }

    [Theory]
    [InlineData("This is a totally valid description")]
    [InlineData("  This is a totally valid description"  )]
    public void Constructor_WithFilledDescription_ShouldTrimDescription(
        string description
    )
    {       
        var equipmentCategory = new EquipmentCategory(
            Guid.NewGuid(),
            "Fenwick",
            description
            );

        Assert.Equal(description.Trim(), equipmentCategory.Description);
    }
}