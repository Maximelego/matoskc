namespace MatosKC.Application.Inspections.Ports;

using MatosKC.Domain.Entities.Inspection;

public interface IInspectionsRepository
{
    Task<Inspection?> GetInspectionByIdAsync(Guid inspectionId, CancellationToken cancellationToken = default);
    Task AddInspectionAsync(Inspection inspection, CancellationToken cancellationToken = default);
    Task UpdateInspectionAsync(Inspection inspection, CancellationToken cancellationToken = default);
}
