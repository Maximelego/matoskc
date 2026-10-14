namespace MatosKC.Application.Inspections.Ports;

using MatosKC.Domain.Entities.Inspection;

public interface IInspectionsRepository
{
    Task<Inspection?> GetInspectionByIdAsync(Guid inspectionId, CancellationToken cancellationToken = default);
    Task<Inspection?> GetForRentalAsync(Guid rentalId, InspectionType type, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Inspection>> ListAsync(
        Guid? rentalId,
        InspectionType? type,
        InspectionStatus? status,
        int offset,
        int limit,
        CancellationToken cancellationToken = default
    );

    Task AddInspectionAsync(Inspection inspection, CancellationToken cancellationToken = default);
    Task UpdateInspectionAsync(Inspection inspection, CancellationToken cancellationToken = default);
}
