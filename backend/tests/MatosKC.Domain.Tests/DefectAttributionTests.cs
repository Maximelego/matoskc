namespace MatosKC.Domain.Tests;

using MatosKC.Domain.Entities.Maintenance;

public sealed class DefectAttributionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DefectsAndOccurrencesRequireAnOperator(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => new EquipmentDefect(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), "Porte", "Rayure", DateTimeOffset.UtcNow, Guid.NewGuid(), name!));
        Assert.ThrowsAny<ArgumentException>(() => new DefectOccurrence(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), "Porte", "Rayure", DateTimeOffset.UtcNow, [Guid.NewGuid()], Guid.NewGuid(), name!));
    }

    [Fact]
    public void DefectsAndOccurrencesRequireAnAccount()
    {
        Assert.Throws<ArgumentException>(() => EquipmentDefect.FromFinding(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), "Porte", "Rayure", DateTimeOffset.UtcNow, Guid.Empty, "Maxime"));
        Assert.Throws<ArgumentException>(() => DefectOccurrence.FromEvidence(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), true, "Porte", "Rayure", DateTimeOffset.UtcNow, [Guid.NewGuid()], Guid.Empty, "Maxime"));
    }
}
