using MatosKC.Domain.Entities.AuthenticationSession;

namespace MatosKC.Application.Auth.Ports;

public interface IAuthenticationSessionRepository
{
    // Completes only once the session is durably saved.
    Task AddAsync(AuthenticationSession session, CancellationToken cancellationToken = default);
    Task<AuthenticationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(AuthenticationSession session, CancellationToken cancellationToken = default);
}
