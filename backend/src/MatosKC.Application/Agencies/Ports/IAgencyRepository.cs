namespace MatosKC.Application.Agencies.Ports;

using MatosKC.Domain.Entities.Agencies;
using MatosKC.Application.Agencies.List;


public interface IAgencyRepository
{
    public Task<Agency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<Agency?> GetByCodeAsync(int code, CancellationToken cancellationToken = default);
    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<Agency[]?> ListByQueryAsync(ListAgenciesQuery query, CancellationToken cancellationToken = default);

    public Task<Agency> AddAsync(Agency agency, CancellationToken cancellationToken = default);
}
