using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Auth.Ports;
using MatosKC.Domain.Entities.Accounts;
namespace MatosKC.Application.Auth.VerifySession;
public sealed class VerifyAuthenticationSessionUseCase(IAuthenticationSessionRepository sessions,
    IAccountRepository accounts, IAgencyRepository agencies, TimeProvider clock)
{
    public async Task<Account?> ExecuteAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await sessions.GetByIdAsync(sessionId, ct);
        if (session == null || !session.IsValid(clock.GetUtcNow().UtcDateTime)) return null;
        var account = await accounts.GetByIdAsync(session.AccountId, ct);
        if (account == null || !account.IsActive) return null;
        if (account.AgencyId is Guid agencyId && !await agencies.ExistsByIdAsync(agencyId, ct)) return null;
        return account;
    }
}
