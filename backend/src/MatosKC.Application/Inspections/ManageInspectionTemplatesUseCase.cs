namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Entities.Inspection;

public sealed class ManageInspectionTemplatesUseCase(InspectionWorkflowContext context)
{
    public async Task<InspectionTemplate> GetAsync(Guid id, CancellationToken ct = default) =>
        await context.Templates.GetTemplateByIdAsync(id, ct)
            ?? throw new InspectionOperationException(404, "Template not found.");

    public async Task<InspectionTemplate> GetLatestAsync(Guid categoryId, CancellationToken ct = default)
    {
        if (!await context.Categories.ExistsByIdAsync(categoryId, ct))
        {
            throw new InspectionOperationException(404, "Equipment category not found.");
        }

        return await context.Templates.GetLatestForCategoryAsync(categoryId, ct)
            ?? throw new InspectionOperationException(404, "No template exists for this category.");
    }

    public Task<InspectionTemplate> CreateAsync(
        CreateTemplateRequest request,
        InspectionActor actor,
        CancellationToken ct = default
    )
    {
        InspectionWorkflowContext.RequireAdministrator(actor);
        ArgumentNullException.ThrowIfNull(request.Points);

        return context.UnitOfWork.ExecuteAsync(async token =>
        {
            var category = await context.Categories.GetByIdAsync(request.EquipmentCategoryId, token);
            if (category is null)
            {
                throw new InspectionOperationException(404, "Equipment category not found.");
            }

            foreach (var photoId in request.Points.Where(point => point is not null && point.PhotoId.HasValue)
                         .Select(point => point.PhotoId!.Value).Distinct())
            {
                var photo = await context.TemplatePhotos.GetByIdAsync(photoId, token)
                    ?? throw new InspectionOperationException(400, "A template point references an unknown illustration.");
                if (photo.AgencyId != category.AgencyId)
                    throw new InspectionOperationException(400, "Template illustrations must belong to the category agency.");
                await using var stored = await context.Storage.RetrieveAsync(photo.ObjectKey, token)
                    ?? throw new InspectionOperationException(409, "A template illustration is unavailable.");
            }

            Guid id = Guid.NewGuid();
            var points = request.Points.Select((point, position) =>
            {
                ArgumentNullException.ThrowIfNull(point);

                return new InspectionTemplatePoint(Guid.NewGuid(), id, point.Description, point.DescriptionDetail, point.PhotoId, position);
            }).ToArray();
            var template = new InspectionTemplate(id, points, request.EquipmentCategoryId);

            if (category.AgencyId != Guid.Empty) template.AssignAgency(category.AgencyId);
            await context.Templates.AddAsync(template, token);

            return template;
        }, ct);
    }
}
