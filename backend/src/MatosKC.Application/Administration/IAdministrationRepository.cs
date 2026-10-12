using MatosKC.Domain.Entities.Agencies;
namespace MatosKC.Application.Administration;

public interface IAdministrationRepository
{
    public Task<Agency> UpdateAgencyAsync(Agency agency, CancellationToken ct);
    public Task DeleteAgencyAsync(Agency agency, CancellationToken ct);
    public Task<bool> AgencyCodeExistsAsync(int code, Guid? exceptId, CancellationToken ct);
    public Task<bool> AgencyHasAccountsAsync(Guid agencyId, CancellationToken ct);
    public Task RevokeAccountSessionsAsync(Guid accountId, DateTime now, CancellationToken ct);
}
