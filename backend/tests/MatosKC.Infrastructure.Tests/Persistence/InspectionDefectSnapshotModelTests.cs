namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Maintenance;
using Microsoft.EntityFrameworkCore;
using MatosKC.Infrastructure.Persistence;

public sealed class InspectionDefectSnapshotModelTests
{
    [Fact]
    public void ModelAndMigration_KeepIndependentSnapshotsAndEvidenceSources()
    {
        using var db = new MatosKCDbContext(new DbContextOptionsBuilder<MatosKCDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var review = db.Model.FindEntityType(typeof(InspectionDefectReview))!;
        Assert.Equal("uuid[]", review.FindProperty("_historicalPhotoIds")!.GetColumnType());
        Assert.Equal("uuid[]", review.FindProperty("_photoIds")!.GetColumnType());
        Assert.Contains(review.GetIndexes(), index => index.IsUnique && index.Properties.Count == 2);
        var finding = db.Model.FindEntityType(typeof(InspectionFinding))!;
        Assert.Equal("uuid[]", finding.FindProperty("_photoIds")!.GetColumnType());
        var media = db.Model.FindEntityType(typeof(InspectionMedia))!;
        Assert.True(media.FindProperty(nameof(InspectionMedia.AnswerId))!.IsNullable);
        Assert.NotNull(media.FindProperty(nameof(InspectionMedia.DefectReviewId)));
        Assert.NotNull(media.FindProperty(nameof(InspectionMedia.FindingId)));
        Assert.True(db.Model.FindEntityType(typeof(DefectOccurrence))!
            .FindProperty(nameof(DefectOccurrence.InspectionAnswerId))!.IsNullable);
    }
}
