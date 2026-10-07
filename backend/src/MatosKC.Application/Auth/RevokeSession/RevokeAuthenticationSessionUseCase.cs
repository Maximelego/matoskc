using MatosKC.Application.Auth.Ports;
namespace MatosKC.Application.Auth.RevokeSession;
public sealed class RevokeAuthenticationSessionUseCase(IAuthenticationSessionRepository sessions, TimeProvider clock)
{
    public async Task ExecuteAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await sessions.GetByIdAsync(sessionId, ct);
        if (session == null) return;
        session.Revoke(clock.GetUtcNow().UtcDateTime);
        await sessions.UpdateAsync(session, ct);
    }
}
