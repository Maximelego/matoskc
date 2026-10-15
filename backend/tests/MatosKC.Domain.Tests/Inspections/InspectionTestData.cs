namespace MatosKC.Domain.Tests.Inspections;

using MatosKC.Domain.Entities.Inspection;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

internal static class InspectionTestData
{
    internal static readonly DateTimeOffset StartedAt =
        new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    internal static InspectionTemplate Template(int pointCount = 1)
    {
        Guid templateId = Guid.NewGuid();
        var points = Enumerable.Range(1, pointCount)
            .Select(index => new InspectionTemplatePoint(
                Guid.NewGuid(), templateId, $"Point {index}", "Le point sera à préciser", null
            ));

        return new InspectionTemplate(templateId, points, Guid.NewGuid());
    }

    internal static InspectionEntity Inspection(
        InspectionTemplate template,
        Guid? rentalId = null,
        InspectionType type = InspectionType.Departure
    ) => new(
        Guid.NewGuid(),
        rentalId ?? Guid.NewGuid(),
        template.Id,
        type,
        Guid.NewGuid(),
        "Maxime",
        StartedAt
    );

    internal static void ValidateSigned(InspectionEntity inspection, InspectionTemplate template, DateTimeOffset time)
    {
        if (inspection.Status == InspectionStatus.Draft)
        {
            inspection.Sign("Client", [1], StartedAt);
        }

        inspection.Validate(template, time);
    }

    internal static InspectionEntity Validated(
        InspectionTemplate template,
        Guid rentalId,
        InspectionType type
    )
    {
        var inspection = Inspection(template, rentalId, type);

        foreach (var point in template.Points)
        {
            inspection.SetAnswer(point.Id, InspectionResult.Compliant, null);
        }

        ValidateSigned(inspection, template, StartedAt.AddMinutes(5));

        return inspection;
    }
}
