using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
namespace MatosKC.Application.Administration;
public interface IAdministrationRepository
{
    Task<Agency> UpdateAgencyAsync(Agency agency, CancellationToken ct);
    Task DeleteAgencyAsync(Agency agency, CancellationToken ct);
    Task<bool> AgencyCodeExistsAsync(int code, Guid? exceptId, CancellationToken ct);
    Task<bool> AgencyHasAccountsAsync(Guid agencyId, CancellationToken ct);
    Task RevokeAccountSessionsAsync(Guid accountId, DateTime now, CancellationToken ct);
}
