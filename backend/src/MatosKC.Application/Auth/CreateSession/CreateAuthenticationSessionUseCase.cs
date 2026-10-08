namespace MatosKC.Application.Auth.CreateSession;

using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Auth.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.AuthenticationSession;

public class CreateAuthenticationSessionUseCase
{
    private readonly IAccountRepository _accounts;
    private readonly IAgencyRepository _agencies;
    private readonly IPasswordHasher _hasher;
    private readonly IAuthenticationSessionRepository _sessions;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _sessionDuration;

    public CreateAuthenticationSessionUseCase(
        IAccountRepository accountRepository,
        IAgencyRepository agencyRepository,
        IPasswordHasher passwordHasher,
        IAuthenticationSessionRepository sessionRepository,
        TimeProvider? timeProvider = null,
        TimeSpan? sessionDuration = null)
    {
        _accounts = accountRepository;
        _agencies = agencyRepository;
        _hasher = passwordHasher;
        _sessions = sessionRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _sessionDuration = sessionDuration ?? TimeSpan.FromHours(1);
        if (_sessionDuration <= TimeSpan.Zero)
            throw new ArgumentException("Session duration must be positive.", nameof(sessionDuration));
    }

    public async Task<AuthenticationSession> ExecuteAsync(
        CreateAuthenticationSessionDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        cancellationToken.ThrowIfCancellationRequested();

        bool hasEmail = !string.IsNullOrWhiteSpace(dto.Email);

        if (dto.AgencyCode.HasValue == hasEmail)
            throw new InvalidOperationException("Provide exactly one of AgencyCode or Email.");
        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new InvalidOperationException("Invalid credentials.");

        Account? account;

        if (dto.AgencyCode.HasValue)
        {
            if (dto.AgencyCode.Value <= 0)
                throw new InvalidOperationException("Invalid credentials.");

            var agency = await _agencies.GetByCodeAsync(dto.AgencyCode.Value, cancellationToken) ?? throw new InvalidOperationException("Invalid credentials.");

            account = await _accounts.GetByAgencyIdAsync(agency.Id, cancellationToken);

            if (account == null || account.Role != Role.Agency || account.AgencyId != agency.Id)
                throw new InvalidOperationException("Invalid credentials.");
        }
        else
        {
            account = await _accounts.GetByEmailAsync(dto.Email!, cancellationToken);
            if (account == null || account.Role == Role.Agency)
                throw new InvalidOperationException("Invalid credentials.");
        }

        if (!account.IsActive)
            throw new InvalidOperationException("Invalid credentials.");

        var verificationResult = _hasher.VerifyPassword(dto.Password, account.HashedPassword);
        if (verificationResult == HashVerificationResult.Failed)
            throw new InvalidOperationException("Invalid credentials.");

        if (verificationResult == HashVerificationResult.NeedsRehash)
        {
            account.UpdateHashedPassword(_hasher.HashPassword(dto.Password));
            await _accounts.UpdateAsync(account, cancellationToken);
        }

        var session = new AuthenticationSession(account.Id, _sessionDuration, _timeProvider.GetUtcNow().UtcDateTime);
        await _sessions.AddAsync(session, cancellationToken);

        return session;
    }
}
