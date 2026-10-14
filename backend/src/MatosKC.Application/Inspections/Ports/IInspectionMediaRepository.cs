namespace MatosKC.Application.Inspections.Ports;

using MatosKC.Domain.Entities.Inspection;

public interface IInspectionMediaRepository
{
    Task<InspectionMedia?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(InspectionMedia media, CancellationToken cancellationToken = default);
}
