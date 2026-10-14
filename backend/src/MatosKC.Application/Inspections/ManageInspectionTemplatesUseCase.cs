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
            if (!await context.Categories.ExistsByIdAsync(request.EquipmentCategoryId, token))
            {
                throw new InspectionOperationException(404, "Equipment category not found.");
            }

            Guid id = Guid.NewGuid();
            var points = request.Points.Select((point, position) =>
            {
                ArgumentNullException.ThrowIfNull(point);

                return new InspectionTemplatePoint(Guid.NewGuid(), id, point.Description, point.PhotoId, position);
            }).ToArray();
            var template = new InspectionTemplate(id, points, request.EquipmentCategoryId);

            await context.Templates.AddAsync(template, token);

            return template;
        }, ct);
    }
}
