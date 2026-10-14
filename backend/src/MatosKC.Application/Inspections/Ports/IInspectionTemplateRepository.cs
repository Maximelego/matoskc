namespace MatosKC.Application.Inspections.Ports;

using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Equipments;

public interface IInspectionTemplateRepository
{
    public Task<InspectionTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    public Task<InspectionTemplate?> GetLatestForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    public Task AddAsync(InspectionTemplate template, CancellationToken cancellationToken = default);
}
