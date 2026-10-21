namespace MatosKC.Application.Inspections;

using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Entities.Inspection;

public sealed class ManageInspectionPhotosUseCase(InspectionWorkflowContext context)
{
    public async Task<InspectionMedia> UploadAsync(
        Guid inspectionId,
        Guid answerId,
        Stream content,
        string fileName,
        string contentType,
        long contentLength,
        InspectionActor actor,
        CancellationToken ct = default
    )
    {
        InspectionWorkflowContext.RequireOperator(actor);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (contentLength is <= 0 or > 10 * 1024 * 1024
            || contentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            throw new InspectionOperationException(400, "Use a JPEG, PNG or WebP image of at most 10 MB.");
        }

        Guid photoId = Guid.NewGuid();
        string key = $"inspection-photos/{inspectionId}/{photoId}";
        bool storageAttempted = false;

        try
        {
            return await context.UnitOfWork.ExecuteAsync(async token =>
            {
                var (inspection, _) = await context.GetInspectionAsync(inspectionId, actor, token);

                if (inspection.Status != InspectionStatus.Draft)
                {
                    throw new InspectionOperationException(409, "A validated inspection cannot accept photos.");
                }

                if (!inspection.Answers.Any(answer => answer.Id == answerId) && !inspection.HasEvidenceTarget(answerId))
                {
                    throw new InspectionOperationException(404, "Inspection answer not found.");
                }

                byte[] bytes = await MatosKC.Application.Files.ImageFileReader.ReadValidatedAsync(
                    content, contentLength, contentType, context.ImageValidator, token);
                using var validated = new MemoryStream(bytes, writable: false);
                storageAttempted = true;
                await context.Storage.StoreAsync(key, validated, contentType, token);
                var media = new InspectionMedia(photoId, inspection.Id, answerId, key,
                    Path.GetFileName(fileName), contentType, contentLength);
                if (inspection.HasEvidenceTarget(answerId))
                {
                    media = InspectionMedia.ForEvidence(photoId, inspection.Id, answerId,
                        inspection.KnownDefects.Any(value => value.Id == answerId), key,
                        Path.GetFileName(fileName), contentType, contentLength);
                    inspection.AddEvidencePhoto(answerId, photoId);
                }
                else inspection.AddAnswerPhoto(answerId, photoId);

                await context.Media.AddAsync(media, token);
                await context.Inspections.UpdateInspectionAsync(inspection, token);

                return media;
            }, ct);
        }
        catch
        {
            if (storageAttempted)
            {
                // Cleanup is independent of a cancelled HTTP request.
                await context.Storage.DeleteAsync(key, CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<StoredObject> DownloadAsync(
        Guid inspectionId,
        Guid photoId,
        InspectionActor actor,
        CancellationToken ct = default
    )
    {
        var (inspection, _) = await context.GetInspectionAsync(inspectionId, actor, ct);
        var media = await context.Media.GetByIdAsync(photoId, ct)
            ?? throw new InspectionOperationException(404, "Inspection photo not found.");

        var historical = inspection.KnownDefects.Any(value => value.HistoricalPhotoIds.Contains(media.Id));
        var attached = inspection.Answers.Any(value => value.PhotosIds.Contains(media.Id))
            || inspection.Findings.Any(value => value.PhotoIds.Contains(media.Id))
            || inspection.KnownDefects.Any(value => value.PhotoIds.Contains(media.Id));
        if (!historical && (media.InspectionId != inspection.Id || !attached))
        {
            throw new InspectionOperationException(404, "Photo is not attached to this inspection.");
        }

        return await context.Storage.RetrieveAsync(media.ObjectKey, ct)
            ?? throw new InspectionOperationException(404, "Photo content not found.");
    }

    public Task<bool> RemoveAsync(Guid inspectionId, Guid photoId, InspectionActor actor, CancellationToken ct = default)
    {
        InspectionWorkflowContext.RequireOperator(actor);

        return context.UnitOfWork.ExecuteAsync(async token =>
        {
            var (inspection, _) = await context.GetInspectionAsync(inspectionId, actor, token);
            var media = await context.Media.GetByIdAsync(photoId, token)
                ?? throw new InspectionOperationException(404, "Inspection photo not found.");

            if (media.InspectionId != inspection.Id)
            {
                throw new InspectionOperationException(404, "Photo does not belong to this inspection.");
            }

            if (media.AnswerId.HasValue) inspection.RemoveAnswerPhoto(media.AnswerId.Value, media.Id);
            else inspection.RemoveEvidencePhoto(media.FindingId ?? media.DefectReviewId!.Value, media.Id);
            await context.Inspections.UpdateInspectionAsync(inspection, token);

            // Keep the object until a separate cleanup can safely delete unreferenced media.
            return true;
        }, ct);
    }
}
