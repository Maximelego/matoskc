namespace MatosKC.Application.Maintenance;

using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Maintenance;

public sealed class QueryEquipmentCareUseCase(InspectionWorkflowContext context)
{
    private async Task RequireEquipmentAsync(Guid id, InspectionActor actor, int offset, int limit, CancellationToken ct)
    {
        InspectionWorkflowContext.RequireReader(actor);

        if (offset < 0 || limit is < 1 or > 100)
        {
            throw new InspectionOperationException(400, "Invalid pagination (limit 1 to 100).");
        }

        if (!await context.Equipment.ExistsByIdAsync(id, ct))
        {
            throw new InspectionOperationException(404, "Equipment not found.");
        }
    }

    public async Task<CarePage<EquipmentDefectResponse>> ListDefectsAsync(Guid id, EquipmentDefectStatus? status,
        int offset, int limit, InspectionActor actor, CancellationToken ct = default)
    {
        await RequireEquipmentAsync(id, actor, offset, limit, ct);

        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new InspectionOperationException(400, "Invalid defect status.");
        }

        var page = await context.Care.ListDefectsAsync(id, status, offset, limit, ct);
        var occurrences = await context.Care.GetOccurrencesAsync(page.Items.Select(defect => defect.Id).ToArray(), ct);
        var items = page.Items.Select(defect => new EquipmentDefectResponse(defect.Id, defect.EquipmentId,
            defect.InspectionId, defect.InspectionAnswerId, defect.Description, defect.Observation,
            defect.ReportedAt, defect.Status, defect.ResolvedByMaintenanceId, defect.ResolvedByAccountId,
            defect.ResolvedAt, occurrences.Where(occurrence => occurrence.DefectId == defect.Id)
                .Select(occurrence => new DefectOccurrenceResponse(occurrence.Id, occurrence.InspectionId,
                    occurrence.InspectionAnswerId, occurrence.Description, occurrence.Observation,
                    occurrence.ObservedAt, occurrence.PhotoIds.ToArray(), occurrence.ReportedByAccountId, occurrence.OperatorFirstName, occurrence.InspectionFindingId, occurrence.InspectionDefectReviewId)).ToArray(), defect.ReportedByAccountId, defect.OperatorFirstName, defect.InspectionFindingId)).ToArray();

        return new(items, page.Total, offset, limit);
    }

    public async Task<CarePage<MaintenanceResponse>> ListMaintenancesAsync(Guid id, int offset, int limit,
        InspectionActor actor, CancellationToken ct = default)
    {
        await RequireEquipmentAsync(id, actor, offset, limit, ct);
        var page = await context.Care.ListMaintenancesAsync(id, offset, limit, ct);

        return new(page.Items.Select(MaintenanceResponse.From).ToArray(), page.Total, offset, limit);
    }

    public async Task<MaintenanceResponse> GetMaintenanceAsync(Guid equipmentId, Guid maintenanceId,
        InspectionActor actor, CancellationToken ct = default)
    {
        await RequireEquipmentAsync(equipmentId, actor, 0, 1, ct);
        var maintenance = await context.Care.GetMaintenanceAsync(maintenanceId, ct);

        if (maintenance is null || maintenance.EquipmentId != equipmentId)
        {
            throw new InspectionOperationException(404, "Intervention not found for this equipment.");
        }

        return MaintenanceResponse.From(maintenance);
    }

    public async Task<MatosKC.Application.Files.Ports.StoredObject> DownloadDefectPhotoAsync(
        Guid equipmentId, Guid defectId, Guid photoId, InspectionActor actor, CancellationToken ct = default)
    {
        await RequireEquipmentAsync(equipmentId, actor, 0, 1, ct);
        var defect = await context.Care.GetDefectAsync(defectId, ct);

        if (defect is null || defect.EquipmentId != equipmentId)
        {
            throw new InspectionOperationException(404, "Defect not found for this equipment.");
        }

        var occurrences = await context.Care.GetOccurrencesAsync([defectId], ct);
        var media = await context.Media.GetByIdAsync(photoId, ct);

        if (media is null || !occurrences.Any(occurrence => occurrence.InspectionAnswerId == media.AnswerId
                && occurrence.InspectionFindingId == media.FindingId && occurrence.InspectionDefectReviewId == media.DefectReviewId
                && occurrence.InspectionId == media.InspectionId && occurrence.PhotoIds.Contains(photoId)))
        {
            throw new InspectionOperationException(404, "Photo not attached to this defect.");
        }

        return await context.Storage.RetrieveAsync(media.ObjectKey, ct)
            ?? throw new InspectionOperationException(404, "Photo content not found.");
    }
}
