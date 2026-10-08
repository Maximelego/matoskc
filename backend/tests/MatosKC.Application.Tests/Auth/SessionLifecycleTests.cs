using MatosKC.Application.Accounts.List;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Agencies.List;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Auth.Ports;
using MatosKC.Application.Auth.RevokeSession;
using MatosKC.Application.Auth.VerifySession;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using Session = MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession;
namespace MatosKC.Application.Tests.Auth;

public sealed class SessionLifecycleTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private readonly Clock _clock = new();
    private readonly Store _store = new();
    private VerifyAuthenticationSessionUseCase Verify() => new(_store, _store, _store, _clock);
    private Session Add(Role role = Role.SuperAdmin)
    {
        _store.Account = new("Compte", role == Role.Agency ? null : "test@example.com", "hash", true, role,
            role == Role.SuperAdmin ? null : _store.Agency.Id);
        var session = new Session(_store.Account.Id, TimeSpan.FromHours(1), _clock.Now.UtcDateTime);
        _store.Session = session;
        return session;
    }
    [Theory]
    [InlineData(Role.SuperAdmin)]
    [InlineData(Role.Admin)]
    [InlineData(Role.Agency)]
    public async Task ValidSession_ReturnsCurrentAccount(Role role)
    {
        var session = Add(role);
        _store.Account!.UpdateDisplayName("Nom actuel");
        Assert.Same(_store.Account, await Verify().ExecuteAsync(session.Id));
    }
    [Fact]
    public async Task UnknownSession_IsRejected() => Assert.Null(await Verify().ExecuteAsync(Guid.NewGuid()));
    [Fact]
    public async Task ExpiredSession_IsRejected()
    {
        var session = Add(); _clock.Now = new DateTimeOffset(session.ExpiresAt);
        Assert.Null(await Verify().ExecuteAsync(session.Id));
    }
    [Fact]
    public async Task RevokedSession_IsRejected()
    {
        var session = Add(); session.Revoke(_clock.Now.UtcDateTime);
        Assert.Null(await Verify().ExecuteAsync(session.Id));
    }
    [Fact]
    public async Task DisabledAccount_IsRejected()
    {
        var session = Add(); _store.Account!.DeactivateAccount();
        Assert.Null(await Verify().ExecuteAsync(session.Id));
    }
    [Fact]
    public async Task DeletedAccount_IsRejected()
    {
        var session = Add(); _store.Account = null;
        Assert.Null(await Verify().ExecuteAsync(session.Id));
    }
    [Fact]
    public async Task MissingAgency_IsRejected()
    {
        var session = Add(Role.Admin); _store.AgencyExists = false;
        Assert.Null(await Verify().ExecuteAsync(session.Id));
    }
    [Fact]
    public async Task Revoke_IsPersistedAndIdempotent()
    {
        var session = Add(); var revoke = new RevokeAuthenticationSessionUseCase(_store, _clock);
        await revoke.ExecuteAsync(session.Id);
        var first = session.RevokedAt; _clock.Now = _clock.Now.AddMinutes(1);
        await revoke.ExecuteAsync(session.Id);
        Assert.Equal(first, session.RevokedAt); Assert.Equal(2, _store.Updates);
        Assert.Null(await Verify().ExecuteAsync(session.Id));
    }
    [Fact]
    public async Task RevokeUnknownSession_IsHarmless()
    {
        await new RevokeAuthenticationSessionUseCase(_store, _clock).ExecuteAsync(Guid.NewGuid());
        Assert.Equal(0, _store.Updates);
    }
    private sealed class Store : IAuthenticationSessionRepository, IAccountRepository, IAgencyRepository
    {
        public Account? Account;
        public Session? Session;
        public Agency Agency = new("Épinal", 83);
        public bool AgencyExists = true;
        public int Updates;
        Task<Session?> IAuthenticationSessionRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Session?.Id == id ? Session : null);
        public Task AddAsync(Session session, CancellationToken cancellationToken = default) { Session = session; return Task.CompletedTask; }
        public Task UpdateAsync(Session session, CancellationToken cancellationToken = default) { Updates++; return Task.CompletedTask; }
        Task<Account?> IAccountRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Account?.Id == id ? Account : null);
        Task<Agency?> IAgencyRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Agency?>(Agency);
        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(AgencyExists && id == Agency.Id);
        public Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Account?> GetByAgencyIdAsync(Guid agencyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Account>> ListByQueryAsync(ListAccountsQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Account account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAgencyAccountAsync(Guid agencyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Agency?> GetByCodeAsync(int code, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Agency[]?> ListByQueryAsync(ListAgenciesQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Agency> AddAsync(Agency agency, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
