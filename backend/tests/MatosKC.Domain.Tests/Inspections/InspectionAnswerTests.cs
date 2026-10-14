namespace MatosKC.Domain.Tests.Inspections;

using MatosKC.Domain.Entities.Inspection;

public class InspectionAnswerTests
{
    [Theory]
    [InlineData(InspectionResult.Compliant)]
    [InlineData(InspectionResult.NonCompliant)]
    public void Constructor_AllowsIncompleteDraftAnswer(InspectionResult result)
    {
        var answer = new InspectionAnswer(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), result, null
        );

        Assert.Equal(result, answer.Result);
        Assert.Null(answer.Comment);
        Assert.Empty(answer.PhotosIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_RejectsEmptyIdentifiers(int emptyIndex)
    {
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        ids[emptyIndex] = Guid.Empty;

        Assert.Throws<ArgumentException>(() => new InspectionAnswer(
            ids[0], ids[1], ids[2], InspectionResult.Compliant, null
        ));
    }

    [Fact]
    public void Constructor_RejectsInvalidResultIncludingRemovedNotApplicable()
    {
        Assert.Throws<ArgumentException>(() => new InspectionAnswer(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (InspectionResult)2, null
        ));
    }

    [Theory]
    [InlineData("  Observation  ", "Observation")]
    [InlineData("   ", null)]
    [InlineData("", null)]
    public void Constructor_NormalizesComment(string comment, string? expected)
    {
        var answer = new InspectionAnswer(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InspectionResult.NonCompliant, comment
        );

        Assert.Equal(expected, answer.Comment);
    }
}
