namespace MatosKC.Domain.Entities.Maintenance;

public sealed class EquipmentMaintenance : BaseEntity
{
    private List<Guid> _resolvedDefectIds = [];

    public Guid EquipmentId { get; private set; }
    public DateTimeOffset PerformedAt { get; private set; }
    public string PerformedBy { get; private set; } = null!;
    public string WorkDescription { get; private set; } = null!;
    public Guid RecordedByAccountId { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public IReadOnlyList<Guid> ResolvedDefectIds => _resolvedDefectIds.AsReadOnly();

    private EquipmentMaintenance() { }

    public static EquipmentMaintenance Record(Guid id, Guid equipmentId, DateTimeOffset performedAt,
        string performedBy, string workDescription, Guid recordedByAccountId,
        DateTimeOffset recordedAt, IReadOnlyList<EquipmentDefect> defects)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(performedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(workDescription);
        ArgumentNullException.ThrowIfNull(defects);

        if (id == Guid.Empty || equipmentId == Guid.Empty || recordedByAccountId == Guid.Empty)
        {
            throw new ArgumentException("Maintenance, equipment and account identifiers are required.");
        }

        if (performedAt == default || recordedAt == default
            || performedBy.Trim().Length > 200 || workDescription.Trim().Length > 10000)
        {
            throw new ArgumentException("Invalid intervention dates or text lengths.");
        }

        if (defects.Any(defect => defect is null || defect.EquipmentId != equipmentId
                || defect.Status != EquipmentDefectStatus.Open || performedAt < defect.ReportedAt)
            || defects.Select(defect => defect.Id).Distinct().Count() != defects.Count)
        {
            throw new InvalidOperationException("Select distinct open defects from this equipment, reported before the intervention.");
        }

        var maintenance = new EquipmentMaintenance
        {
            Id = id,
            EquipmentId = equipmentId,
            PerformedAt = performedAt.ToUniversalTime(),
            PerformedBy = performedBy.Trim(),
            WorkDescription = workDescription.Trim(),
            RecordedByAccountId = recordedByAccountId,
            RecordedAt = recordedAt.ToUniversalTime(),
            _resolvedDefectIds = defects.Select(defect => defect.Id).ToList()
        };

        // Validate the whole intervention before mutating any defect.
        foreach (var defect in defects)
        {
            defect.Resolve(id, recordedByAccountId, performedAt);
        }

        return maintenance;
    }
}
