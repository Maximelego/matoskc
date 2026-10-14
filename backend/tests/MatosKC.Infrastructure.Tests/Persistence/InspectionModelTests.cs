namespace MatosKC.Infrastructure.Tests.Persistence;

using Microsoft.EntityFrameworkCore;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public sealed class InspectionModelTests
{
    [Fact]
    public void Model_MapsWorkflowEntitiesCollectionsAndConcurrencyTokens()
    {
        using var db = new MatosKCDbContext(new DbContextOptionsBuilder<MatosKCDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var inspection = db.Model.FindEntityType(typeof(InspectionEntity))!;
        Assert.NotNull(inspection.FindPrimaryKey());
        Assert.Equal("_answers", inspection.FindNavigation(nameof(InspectionEntity.Answers))!.FieldInfo!.Name);
        Assert.True(inspection.FindProperty("xmin")!.IsConcurrencyToken);
        var liveTypeIndex = inspection.GetIndexes().Single(index => index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(new[] { "RentalId", "Type" }));
        Assert.Equal("status <> 'Cancelled'", liveTypeIndex.GetFilter());
        Assert.Equal(1000, inspection.FindProperty(nameof(InspectionEntity.CancellationReason))!.GetMaxLength());
        Assert.True(db.Model.FindEntityType(typeof(Rental))!.FindProperty("xmin")!.IsConcurrencyToken);
        Assert.True(db.Model.FindEntityType(typeof(Equipment))!.FindProperty("xmin")!.IsConcurrencyToken);
        var answers = db.Model.FindEntityType(typeof(InspectionAnswer))!;
        Assert.Equal("uuid[]", answers.FindProperty("_photos")!.GetColumnType());
        Assert.Contains(answers.GetIndexes(), index => index.IsUnique);
        var rentals = db.Model.FindEntityType(typeof(Rental))!;
        Assert.Contains(rentals.GetIndexes(), index => index.IsUnique
            && index.GetFilter() == "status IN ('Draft', 'Active')");
        Assert.Equal("_points", db.Model.FindEntityType(typeof(InspectionTemplate))!
            .FindNavigation(nameof(InspectionTemplate.Points))!.FieldInfo!.Name);
    }
}
