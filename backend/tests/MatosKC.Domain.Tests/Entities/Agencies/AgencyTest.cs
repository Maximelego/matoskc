using MatosKC.Domain.Entities.Agencies;

namespace MatosKC.Domain.Tests.Entities.Agencies;

public class AgencyTests
{
    [Fact]
    public void Constructor_WithValidParameters_PreservesValues()
    {
        Guid id = Guid.NewGuid();

        var agency = new Agency(id, "Épinal", 83);

        Assert.Equal(id, agency.Id);
        Assert.Equal("Épinal", agency.Name);
        Assert.Equal(83, agency.Code);
    }

    [Fact]
    public void Constructor_WithoutId_GeneratesDistinctNonEmptyIds()
    {
        var first = new Agency("Épinal", 83);
        var second = new Agency("Nancy", 84);

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(Guid.Empty, second.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Agency(Guid.Empty, "Épinal", 83));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Agency(Guid.NewGuid(), name!, 83));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidCode_Throws(int code)
    {
        Assert.Throws<ArgumentException>(() =>
            new Agency(Guid.NewGuid(), "Épinal", code));
    }
}

