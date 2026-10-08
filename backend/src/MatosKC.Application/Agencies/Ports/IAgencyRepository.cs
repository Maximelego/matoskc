namespace MatosKC.Application.Agencies.Ports;

using MatosKC.Application.Agencies.List;
using MatosKC.Domain.Entities.Agencies;


public interface IAgencyRepository
{
    public Task<Agency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<Agency?> GetByCodeAsync(int code, CancellationToken cancellationToken = default);
    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<Agency[]?> ListByQueryAsync(ListAgenciesQuery query, CancellationToken cancellationToken = default);

    public Task<Agency> AddAsync(Agency agency, CancellationToken cancellationToken = default);
}
