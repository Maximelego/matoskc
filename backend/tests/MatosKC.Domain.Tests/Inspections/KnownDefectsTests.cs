namespace MatosKC.Domain.Tests.Inspections;

using MatosKC.Domain.Entities.Inspection;

public sealed class KnownDefectsTests
{
    private static (Inspection Inspection, InspectionTemplate Template) Create()
    {
        var templateId = Guid.NewGuid();
        var template = new InspectionTemplate(templateId,
            [new InspectionTemplatePoint(Guid.NewGuid(), templateId, "Freinage", "Contrôle", null)], Guid.NewGuid());
        var inspection = new Inspection(Guid.NewGuid(), Guid.NewGuid(), templateId, InspectionType.Departure,
            Guid.NewGuid(), "Maxime", DateTimeOffset.UtcNow);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);
        return (inspection, template);
    }

    [Fact]
    public void SnapshotCopiesPhotos_AndIdenticalPresentationPreservesEvidence()
    {
        var (inspection, _) = Create();
        var id = Guid.NewGuid();
        var photos = new List<Guid> { Guid.NewGuid() };
        inspection.PresentDefect(id, "Porte", "Rayure", inspection.StartedAt, photos);
        photos.Clear();
        inspection.PresentDefect(id, "Porte", "Rayure", inspection.StartedAt, inspection.KnownDefects[0].HistoricalPhotoIds);
        var review = Assert.Single(inspection.KnownDefects);
        Assert.Equal("Porte", review.Description);
        Assert.Equal("Rayure", review.HistoricalObservation);
        Assert.Single(review.HistoricalPhotoIds);
    }

    [Theory]
    [InlineData(DefectReviewResult.Unchanged)]
    [InlineData(DefectReviewResult.MaintenanceCheckRequired)]
    public void KnownDefectDoesNotRequireNewPhoto(DefectReviewResult result)
    {
        var (inspection, template) = Create();
        var id = Guid.NewGuid();
        inspection.PresentDefect(id, "Porte", "Rayure", inspection.StartedAt, [Guid.NewGuid()]);
        inspection.ReviewDefect(id, result, null);
        inspection.Sign("Client", [1], inspection.StartedAt);
        inspection.Validate(template, inspection.StartedAt);
        Assert.Equal(InspectionStatus.Validated, inspection.Status);
        Assert.Throws<InvalidOperationException>(() => inspection.ReviewDefect(id, result, null));
        Assert.Throws<InvalidOperationException>(() => inspection.PresentDefect(Guid.NewGuid(), "X", "Y", inspection.StartedAt, []));
        Assert.Throws<InvalidOperationException>(() => inspection.AddEvidencePhoto(inspection.KnownDefects[0].Id, Guid.NewGuid()));
    }

    [Fact]
    public void UnreviewedDefectPreventsValidation()
    {
        var (inspection, template) = Create();
        inspection.PresentDefect(Guid.NewGuid(), "Porte", "Rayure", inspection.StartedAt, []);
        inspection.Sign("Client", [1], inspection.StartedAt);
        Assert.Throws<InvalidOperationException>(() => inspection.Validate(template, inspection.StartedAt));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("Plus grande", false)]
    public void WorseningRequiresObservationAndFreshPhoto(string? observation, bool photo)
    {
        var (inspection, template) = Create();
        var id = Guid.NewGuid();
        inspection.PresentDefect(id, "Porte", "Rayure", inspection.StartedAt, [Guid.NewGuid()]);
        inspection.ReviewDefect(id, DefectReviewResult.Worsened, observation);
        if (photo) inspection.AddEvidencePhoto(inspection.KnownDefects[0].Id, Guid.NewGuid());
        inspection.Sign("Client", [1], inspection.StartedAt);
        Assert.Throws<InvalidOperationException>(() => inspection.Validate(template, inspection.StartedAt));
    }

    [Fact]
    public void MultipleFindingsOnOnePointEachRequireEvidence_AndInvalidateSignature()
    {
        var (inspection, template) = Create();
        var pointId = template.Points[0].Id;
        inspection.SetAnswer(pointId, InspectionResult.NonCompliant, null);
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        inspection.SetFinding(first, pointId, "Rayure");
        inspection.SetFinding(second, pointId, "Enfoncement");
        inspection.AddEvidencePhoto(first, Guid.NewGuid());
        inspection.Sign("Client", [1], inspection.StartedAt);
        Assert.Throws<InvalidOperationException>(() => inspection.Validate(template, inspection.StartedAt));
        inspection.AddEvidencePhoto(second, Guid.NewGuid());
        Assert.Null(inspection.SignaturePng);
        inspection.Sign("Client", [1], inspection.StartedAt);
        inspection.Validate(template, inspection.StartedAt);
        Assert.Equal(2, inspection.Findings.Count);
        Assert.Throws<InvalidOperationException>(() => inspection.RemoveFinding(first));
        Assert.Throws<InvalidOperationException>(() => inspection.SetFinding(first, pointId, "Modifié"));
    }

    [Fact]
    public void FindingCannotBeHiddenByACompliantAnswer()
    {
        var (inspection, template) = Create();
        var id = Guid.NewGuid();
        inspection.SetFinding(id, template.Points[0].Id, "Rayure");
        inspection.AddEvidencePhoto(id, Guid.NewGuid());
        inspection.Sign("Client", [1], inspection.StartedAt);
        Assert.Throws<InvalidOperationException>(() => inspection.Validate(template, inspection.StartedAt));
    }
}
