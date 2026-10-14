namespace MatosKC.Application.Rentals.Ports;

using MatosKC.Domain.Entities.Rental;

public interface IRentalRepository
{
    public Task<Rental?> GetByIdAsync(Guid rentalId, CancellationToken cancellationToken = default);
    public Task<Rental?> GetActiveForEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    Task<Rental?> GetOpenForEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default);

    public Task AddAsync(Rental rental, CancellationToken cancellationToken = default);
    public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default);
}
