namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Domain.Entities.Maintenance;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class EquipmentCareModelTests
{
    [Fact]
    public void ModelAndMigration_PreserveCollectionsUniquenessAndConcurrency()
    {
        using var db = new MatosKCDbContext(new DbContextOptionsBuilder<MatosKCDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var defect = db.Model.FindEntityType(typeof(EquipmentDefect))!;
        Assert.True(defect.FindProperty("xmin")!.IsConcurrencyToken);
        Assert.Contains(defect.GetIndexes(), index => index.IsUnique
            && index.Properties.Single().Name == nameof(EquipmentDefect.InspectionAnswerId));
        var occurrence = db.Model.FindEntityType(typeof(DefectOccurrence))!;
        Assert.Equal("uuid[]", occurrence.FindProperty("_photoIds")!.GetColumnType());
        Assert.Contains(occurrence.GetIndexes(), index => index.IsUnique
            && index.Properties.Single().Name == nameof(DefectOccurrence.InspectionAnswerId));
        var maintenance = db.Model.FindEntityType(typeof(EquipmentMaintenance))!;
        Assert.Equal("uuid[]", maintenance.FindProperty("_resolvedDefectIds")!.GetColumnType());
        Assert.NotNull(db.Model.FindEntityType(typeof(InspectionAnswer))!.FindProperty(nameof(InspectionAnswer.ExistingDefectId)));
    }
}
