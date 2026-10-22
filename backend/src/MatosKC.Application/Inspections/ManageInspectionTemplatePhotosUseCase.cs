namespace MatosKC.Application.Inspections;

using MatosKC.Application.Files;
using MatosKC.Application.Files.Ports;
using MatosKC.Domain.Entities.Inspection;

public sealed record InspectionTemplatePhotoResponse(
    Guid Id, string FileName, string ContentType, long ContentLength, DateTimeOffset CreatedAt, Guid AgencyId = default
);

public sealed class ManageInspectionTemplatePhotosUseCase(InspectionWorkflowContext context)
{
    public async Task<InspectionTemplatePhotoResponse> UploadAsync(
        Stream content, string fileName, string contentType, long contentLength,
        InspectionActor actor, CancellationToken ct = default, Guid? agencyId = null
    )
    {
        InspectionWorkflowContext.RequireAdministrator(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        contentType = contentType.Trim().ToLowerInvariant();
        byte[] bytes = await ImageFileReader.ReadValidatedAsync(content, contentLength, contentType,
            context.ImageValidator, ct);
        Guid id = Guid.NewGuid();
        var photo = new InspectionTemplatePhoto(id, $"inspection-template-photos/{id}",
            Path.GetFileName(fileName), contentType, bytes.LongLength, actor.AccountId, context.Time.GetUtcNow());
        var owner = context.Scope?.ResolveOwner(agencyId) ?? agencyId;
        if (owner.HasValue && !await context.Agencies.ExistsByIdAsync(owner.Value, ct))
            throw new InspectionOperationException(404, "Agency not found.");
        if (owner.HasValue) photo.AssignAgency(owner.Value);
        bool storageAttempted = false;

        try
        {
            return await context.UnitOfWork.ExecuteAsync(async token =>
            {
                using var stream = new MemoryStream(bytes, writable: false);
                storageAttempted = true;
                await context.Storage.StoreAsync(photo.ObjectKey, stream, photo.ContentType, token);
                await context.TemplatePhotos.AddAsync(photo, token);

                return new InspectionTemplatePhotoResponse(photo.Id, photo.FileName,
                    photo.ContentType, photo.ContentLength, photo.CreatedAt, photo.AgencyId);
            }, ct);
        }
        catch
        {
            if (storageAttempted)
            {
                await context.Storage.DeleteAsync(photo.ObjectKey, CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<StoredObject> DownloadAsync(Guid id, InspectionActor actor, CancellationToken ct = default)
    {
        InspectionWorkflowContext.RequireReader(actor);
        var photo = await context.TemplatePhotos.GetByIdAsync(id, ct)
            ?? throw new InspectionOperationException(404, "Template illustration not found.");

        if (photo.AgencyId != Guid.Empty && actor.Role != MatosKC.Domain.Entities.Accounts.Role.SuperAdmin
            && photo.AgencyId != actor.AgencyId)
            throw new InspectionOperationException(403, "Illustration belongs to another agency.");
        return await context.Storage.RetrieveAsync(photo.ObjectKey, ct)
            ?? throw new InspectionOperationException(404, "Template illustration content not found.");
    }
}
