namespace MatosKC.Application.Maintenance;

using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Maintenance;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public static class RecordInspectionDefects
{
    // Called within the inspection validation transaction, after validation succeeded.
    public static async Task ExecuteAsync(InspectionWorkflowContext context, InspectionEntity inspection,
        Guid equipmentId, InspectionTemplate template, DateTimeOffset now, CancellationToken ct)
    {
        foreach (var review in inspection.KnownDefects.Where(value => value.Result == DefectReviewResult.Worsened))
        {
            await RequireOpenAsync(context.Care, review.DefectId, equipmentId, ct);
            await context.Care.AddOccurrenceAsync(DefectOccurrence.FromEvidence(Guid.NewGuid(), review.DefectId,
                inspection.Id, review.Id, true, review.Description, review.Observation!, now, review.PhotoIds), ct);
        }
        foreach (var finding in inspection.Findings)
        {
            var point = template.Points.Single(value => value.Id == finding.TemplatePointId);
            var defect = EquipmentDefect.FromFinding(Guid.NewGuid(), equipmentId, inspection.Id,
                finding.Id, point.Description, finding.Observation, now);
            await context.Care.AddDefectAsync(defect, ct);
            await context.Care.AddOccurrenceAsync(DefectOccurrence.FromEvidence(Guid.NewGuid(), defect.Id,
                inspection.Id, finding.Id, false, point.Description, finding.Observation, now, finding.PhotoIds), ct);
        }
        foreach (var answer in inspection.Answers.Where(answer => answer.Result == InspectionResult.NonCompliant
            && !inspection.Findings.Any(value => value.TemplatePointId == answer.TemplatePointId)))
        {
            var point = template.Points.Single(point => point.Id == answer.TemplatePointId);
            EquipmentDefect defect;

            if (answer.ExistingDefectId.HasValue)
            {
                defect = await RequireOpenAsync(context.Care, answer.ExistingDefectId.Value, equipmentId, ct);
            }
            else
            {
                defect = new EquipmentDefect(Guid.NewGuid(), equipmentId, inspection.Id,
                    answer.Id, point.Description, answer.Comment!, now);
                await context.Care.AddDefectAsync(defect, ct);
            }

            await context.Care.AddOccurrenceAsync(new DefectOccurrence(Guid.NewGuid(), defect.Id,
                inspection.Id, answer.Id, point.Description, answer.Comment!, now, answer.PhotosIds), ct);
        }
    }

    public static async Task<EquipmentDefect> RequireOpenAsync(IEquipmentCareRepository care,
        Guid id, Guid equipmentId, CancellationToken ct)
    {
        var defect = await care.GetDefectAsync(id, ct);

        if (defect is null || defect.EquipmentId != equipmentId)
        {
            throw new InspectionOperationException(404, "Defect not found for this equipment.");
        }

        if (defect.Status != EquipmentDefectStatus.Open)
        {
            throw new InspectionOperationException(409, "The selected defect has already been resolved.");
        }

        return defect;
    }
}
