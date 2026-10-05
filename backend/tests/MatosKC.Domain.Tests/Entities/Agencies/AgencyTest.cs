using MatosKC.Domain.Entities.Agencies;
using Xunit;

namespace MatosKC.Domain.Tests.Entities.Agencies;

public class AgencyTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_WithValidParameters_PreservesValues(bool isActive)
    {
        Guid id = Guid.NewGuid();

        var agency = new Agency(id, "Épinal", 83, isActive);

        Assert.Equal(id, agency.Id);
        Assert.Equal("Épinal", agency.Name);
        Assert.Equal(83, agency.Code);
        Assert.Equal(isActive, agency.IsActive);
    }

    [Fact]
    public void Constructor_WithoutId_GeneratesDistinctNonEmptyIds()
    {
        var first = new Agency("Épinal", 83, true);
        var second = new Agency("Nancy", 84, true);

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(Guid.Empty, second.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Agency(Guid.Empty, "Épinal", 83, true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Agency(Guid.NewGuid(), name!, 83, true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidCode_Throws(int code)
    {
        Assert.Throws<ArgumentException>(() =>
            new Agency(Guid.NewGuid(), "Épinal", code, true));
    }
}
