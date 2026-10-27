namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Maintenance;

internal static class OpenInspectionDefects
{
    public static async Task<IReadOnlyList<EquipmentDefect>> ListAsync(
        InspectionWorkflowContext context, Guid equipmentId, CancellationToken ct)
    {
        var defects = new List<EquipmentDefect>();
        for (var offset = 0; ; offset += 100)
        {
            var page = await context.Care.ListDefectsAsync(equipmentId, EquipmentDefectStatus.Open, offset, 100, ct);
            defects.AddRange(page.Items);
            if (offset + page.Items.Count >= page.Total || page.Items.Count == 0) return defects;
        }
    }

    public static async Task CaptureAsync(InspectionWorkflowContext context, Inspection inspection,
        Guid equipmentId, CancellationToken ct)
    {
        var defects = await ListAsync(context, equipmentId, ct);
        var occurrences = await context.Care.GetOccurrencesAsync(defects.Select(value => value.Id).ToArray(), ct);
        foreach (var defect in defects)
        {
            var last = occurrences.Where(value => value.DefectId == defect.Id)
                .OrderByDescending(value => value.ObservedAt).ThenByDescending(value => value.Id).FirstOrDefault();
            inspection.PresentDefect(defect.Id, last?.Description ?? defect.Description,
                last?.Observation ?? defect.Observation, last?.ObservedAt ?? defect.ReportedAt,
                last?.PhotoIds ?? Array.Empty<Guid>());
        }
    }

    public static async Task EnsureCurrentAsync(InspectionWorkflowContext context, Inspection inspection,
        Guid equipmentId, CancellationToken ct)
    {
        var defects = await ListAsync(context, equipmentId, ct);
        EnsureConsistent(inspection, defects);
        var occurrences = await context.Care.GetOccurrencesAsync(defects.Select(value => value.Id).ToArray(), ct);
        foreach (var defect in defects)
        {
            var review = inspection.KnownDefects.Single(value => value.DefectId == defect.Id);
            var last = occurrences.Where(value => value.DefectId == defect.Id)
                .OrderByDescending(value => value.ObservedAt).ThenByDescending(value => value.Id).FirstOrDefault();
            if (review.Description != (last?.Description ?? defect.Description)
                || review.ObservedAt != (last?.ObservedAt ?? defect.ReportedAt)
                || review.HistoricalObservation != (last?.Observation ?? defect.Observation)
                || !review.HistoricalPhotoIds.SequenceEqual(last?.PhotoIds ?? Array.Empty<Guid>()))
                throw new InspectionOperationException(409, "A known defect changed. Save the draft, review it and sign again.");
        }
    }

    public static void EnsureConsistent(Inspection inspection,
        IReadOnlyList<EquipmentDefect> defects)
    {
        // Live additions must be presented and reviewed before signing again. Resolved defects
        // already presented remain in the historical snapshot and are never silently removed.
        if (defects.Any(defect => !inspection.KnownDefects.Any(review => review.DefectId == defect.Id)))
            throw new InspectionOperationException(409, "New known defects exist. Save the draft, review them and sign again.");
    }
}
