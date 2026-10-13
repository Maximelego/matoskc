namespace MatosKC.Domain.Tests.Clients;

using MatosKC.Domain.Entities.Clients;

public class ClientTests
{
    private static CommandNumber Number() => new(83, "001234");

    [Fact]
    public void Constructor_PreservesCommandAndNormalizesContactDetails()
    {
        var number = Number();
        var client = new Client(
            Guid.NewGuid(), " Maxime ", " maxime@example.test ", " 0612345678 ", number
        );

        Assert.Same(number, client.CommandNumber);
        Assert.Equal("Maxime", client.DisplayName);
        Assert.Equal("maxime@example.test", client.Email);
        Assert.Equal("0612345678", client.Phone);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(0, " ")]
    [InlineData(1, "")]
    [InlineData(1, null)]
    [InlineData(2, " ")]
    [InlineData(2, null)]
    public void Constructor_RejectsMissingContactDetails(int index, string? value)
    {
        string?[] values = ["Maxime", "maxime@example.test", "0612345678"];
        values[index] = value;

        Assert.ThrowsAny<ArgumentException>(() => new Client(
            Guid.NewGuid(), values[0]!, values[1]!, values[2]!, Number()
        ));
    }

    [Fact]
    public void Constructor_RejectsEmptyIdAndMissingCommand()
    {
        Assert.Throws<ArgumentException>(() => new Client(
            Guid.Empty, "Maxime", "maxime@example.test", "0612345678", Number()
        ));
        Assert.Throws<ArgumentNullException>(() => new Client(
            Guid.NewGuid(), "Maxime", "maxime@example.test", "0612345678", null!
        ));
    }

    [Fact]
    public void CommandNumber_PreservesLeadingZeroesAndUsesValueEquality()
    {
        var first = Number();
        var second = Number();

        Assert.Equal("001234", first.Value);
        Assert.Equal(83, first.AgencyCode);
        Assert.Equal(first, second);
        Assert.NotEqual(first, new CommandNumber(33, "001234"));
    }

    [Theory]
    [InlineData(0, "001234")]
    [InlineData(-1, "001234")]
    [InlineData(83, null)]
    [InlineData(83, "")]
    [InlineData(83, "      ")]
    [InlineData(83, "12345")]
    public void CommandNumber_RejectsInvalidAgencyAndReference(int agencyCode, string? value)
    {
        Assert.Throws<ArgumentException>(() => new CommandNumber(agencyCode, value!));
    }
}
