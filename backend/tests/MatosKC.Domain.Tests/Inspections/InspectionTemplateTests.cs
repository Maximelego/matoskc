namespace MatosKC.Domain.Tests.Inspections;

using MatosKC.Domain.Entities.Inspection;

public class InspectionTemplateTests
{
    [Fact]
    public void Constructor_CopiesPointsAndExposesReadOnlyCollection()
    {
        Guid id = Guid.NewGuid();
        var point = new InspectionTemplatePoint(Guid.NewGuid(), id, "Contrôle", null);
        var supplied = new List<InspectionTemplatePoint> { point };
        var template = new InspectionTemplate(id, supplied, Guid.NewGuid());

        supplied.Clear();

        Assert.Same(point, Assert.Single(template.Points));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<InspectionTemplatePoint>)template.Points).Clear());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("nullPoint")]
    [InlineData("foreignPoint")]
    [InlineData("duplicate")]
    public void Constructor_RejectsInvalidPoints(string error)
    {
        Guid id = Guid.NewGuid();
        var point = new InspectionTemplatePoint(Guid.NewGuid(), id, "Contrôle", null);
        IEnumerable<InspectionTemplatePoint>? points = error switch
        {
            "null" => null,
            "empty" => [],
            "nullPoint" => [null!],
            "foreignPoint" => [new InspectionTemplatePoint(Guid.NewGuid(), Guid.NewGuid(), "Autre", null)],
            "duplicate" => [point, point],
            _ => throw new InvalidOperationException()
        };

        Assert.ThrowsAny<ArgumentException>(() =>
            new InspectionTemplate(id, points!, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_RejectsEmptyTemplateAndCategoryIds()
    {
        var template = InspectionTestData.Template();

        Assert.Throws<ArgumentException>(() =>
            new InspectionTemplate(Guid.Empty, template.Points, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() =>
            new InspectionTemplate(template.Id, template.Points, Guid.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Point_RejectsBlankDescription(string? description)
    {
        Assert.Throws<ArgumentException>(() => new InspectionTemplatePoint(
            Guid.NewGuid(), Guid.NewGuid(), description!, null
        ));
    }

    [Fact]
    public void Point_RejectsEmptyIdentifiersIncludingOptionalIllustration()
    {
        Assert.Throws<ArgumentException>(() =>
            new InspectionTemplatePoint(Guid.Empty, Guid.NewGuid(), "Contrôle", null));
        Assert.Throws<ArgumentException>(() =>
            new InspectionTemplatePoint(Guid.NewGuid(), Guid.Empty, "Contrôle", null));
        Assert.Throws<ArgumentException>(() =>
            new InspectionTemplatePoint(Guid.NewGuid(), Guid.NewGuid(), "Contrôle", Guid.Empty));
    }
}
