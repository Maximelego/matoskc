namespace MatosKC.Application.Inspections.Ports;

using MatosKC.Domain.Entities.Inspection;

public interface IInspectionTemplatePhotoRepository
{
    Task<InspectionTemplatePhoto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(InspectionTemplatePhoto photo, CancellationToken ct = default);
}
