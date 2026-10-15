namespace MatosKC.Domain.Tests;

using MatosKC.Domain.Entities.Maintenance;

public sealed class EquipmentCareTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    private static EquipmentDefect Defect(Guid equipmentId) => new(Guid.NewGuid(), equipmentId,
        Guid.NewGuid(), Guid.NewGuid(), "Tuyau", "Déchiré", Now.AddHours(-1));

    [Fact]
    public void Intervention_ResolvesSeveralDefects_WithoutChangingTheirSource()
    {
        Guid equipmentId = Guid.NewGuid();
        var first = Defect(equipmentId);
        var second = Defect(equipmentId);
        Guid sourceId = first.InspectionAnswerId;
        Guid actorId = Guid.NewGuid();

        var maintenance = EquipmentMaintenance.Record(Guid.NewGuid(), equipmentId, Now,
            " Prestataire ", " Remplacement des pièces ", actorId, Now, [first, second]);

        Assert.Equal("Prestataire", maintenance.PerformedBy);
        Assert.Equal(2, maintenance.ResolvedDefectIds.Count);
        Assert.All(new[] { first, second }, defect =>
        {
            Assert.Equal(EquipmentDefectStatus.Resolved, defect.Status);
            Assert.Equal(maintenance.Id, defect.ResolvedByMaintenanceId);
            Assert.Equal(actorId, defect.ResolvedByAccountId);
            Assert.Equal(Now, defect.ResolvedAt);
        });
        Assert.Equal(sourceId, first.InspectionAnswerId);
        Assert.Equal("Déchiré", first.Observation);
    }

    [Fact]
    public void PreventiveIntervention_DoesNotRequireAnyDefect()
    {
        var maintenance = EquipmentMaintenance.Record(Guid.NewGuid(), Guid.NewGuid(), Now,
            "Atelier", "Graissage", Guid.NewGuid(), Now, []);

        Assert.Empty(maintenance.ResolvedDefectIds);
    }

    [Fact]
    public void InvalidDefectSelection_IsRejectedBeforeAnyResolution()
    {
        Guid equipmentId = Guid.NewGuid();
        var valid = Defect(equipmentId);
        var foreign = Defect(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => EquipmentMaintenance.Record(Guid.NewGuid(),
            equipmentId, Now, "Atelier", "Travaux", Guid.NewGuid(), Now, [valid, foreign]));
        Assert.Equal(EquipmentDefectStatus.Open, valid.Status);
        Assert.Null(valid.ResolvedAt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DuplicateOrResolvedDefect_IsRejected(bool duplicate)
    {
        Guid equipmentId = Guid.NewGuid();
        var defect = Defect(equipmentId);

        if (!duplicate)
        {
            EquipmentMaintenance.Record(Guid.NewGuid(), equipmentId, Now, "Atelier", "Travaux",
                Guid.NewGuid(), Now, [defect]);
        }

        Assert.Throws<InvalidOperationException>(() => EquipmentMaintenance.Record(Guid.NewGuid(),
            equipmentId, Now, "Atelier", "Travaux", Guid.NewGuid(), Now,
            duplicate ? new[] { defect, defect } : new[] { defect }));
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(1)]
    public void InterventionCannotPrecedeDefectOrBeInFuture(int offsetHours)
    {
        Guid equipmentId = Guid.NewGuid();
        var defect = Defect(equipmentId);

        Assert.ThrowsAny<Exception>(() => EquipmentMaintenance.Record(Guid.NewGuid(), equipmentId,
            Now.AddHours(offsetHours), "Atelier", "Travaux", Guid.NewGuid(), Now, [defect]));
        Assert.Equal(EquipmentDefectStatus.Open, defect.Status);
    }

    [Fact]
    public void Collections_DefensivelyCopyInputAndCannotBeCleared()
    {
        var photos = new List<Guid> { Guid.NewGuid() };
        var occurrence = new DefectOccurrence(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "Tuyau", "Déchiré", Now, photos);
        photos.Clear();
        Assert.Single(occurrence.PhotoIds);
        Assert.Throws<NotSupportedException>(() => ((IList<Guid>)occurrence.PhotoIds).Clear());

        Guid equipmentId = Guid.NewGuid();
        var maintenance = EquipmentMaintenance.Record(Guid.NewGuid(), equipmentId, Now,
            "Atelier", "Réparation", Guid.NewGuid(), Now, [Defect(equipmentId)]);
        Assert.Throws<NotSupportedException>(() => ((IList<Guid>)maintenance.ResolvedDefectIds).Clear());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingWorkDescription_IsRejected(string description)
    {
        Assert.Throws<ArgumentException>(() => EquipmentMaintenance.Record(Guid.NewGuid(), Guid.NewGuid(),
            Now, "Atelier", description, Guid.NewGuid(), Now, []));
    }
}
