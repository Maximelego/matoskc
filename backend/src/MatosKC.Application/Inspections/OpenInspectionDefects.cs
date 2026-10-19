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

    public static void EnsureConsistent(Inspection inspection, InspectionTemplate template,
        IReadOnlyList<EquipmentDefect> defects)
    {
        foreach (var answer in inspection.Answers.Where(answer => answer.Result == InspectionResult.Compliant))
        {
            var point = template.Points.Single(point => point.Id == answer.TemplatePointId);
            if (defects.Any(defect => string.Equals(defect.Description.Trim(), point.Description.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
                throw new InspectionOperationException(409,
                    "A matching defect is still open. Record it as non-compliant or resolve it through maintenance.");
        }
    }
}
