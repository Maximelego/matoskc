using MatosKC.Application.Accounts.List;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Agencies.List;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Auth.CreateSession;
using MatosKC.Application.Auth.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using Session = MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession;

namespace MatosKC.Application.Tests.Auth.CreateSession;

public class CreateAuthenticationSessionUseCaseTests
{
    private readonly AccountsStub _accounts = new();
    private readonly AgenciesStub _agencies = new();
    private readonly HasherStub _hasher = new();
    private readonly SessionsStub _sessions = new();
    private CreateAuthenticationSessionUseCase UseCase() => new(_accounts, _agencies, _hasher, _sessions);

    private Account SetAccount(Role role, bool active = true)
    {
        var account = new Account("Test", role == Role.Agency ? null : "test@example.com",
            "stored-hash", active, role, role == Role.SuperAdmin ? null : _agencies.Agency!.Id);
        _accounts.Account = account;
        return account;
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.SuperAdmin)]
    public async Task ExecuteAsync_WithValidEmail_CreatesSessionForAccount(Role role)
    {
        Account account = SetAccount(role);
        var session = await UseCase().ExecuteAsync(new(null, account.Email, "correct-password"));
        Assert.Equal(account.Id, session.AccountId);
        Assert.True(session.IsValid());
        Assert.Same(session, Assert.Single(_sessions.Sessions));
        Assert.Equal(TimeSpan.FromHours(1), session.ExpiresAt - session.CreatedAt);
        Assert.Equal(account.Email, _accounts.LastEmail);
        Assert.Equal("correct-password", _hasher.LastPassword);
        Assert.Equal(account.HashedPassword, _hasher.LastHash);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidAgency_CreatesSessionForSharedAccount()
    {
        Account account = SetAccount(Role.Agency);
        var session = await UseCase().ExecuteAsync(new(83, null, "correct-password"));
        Assert.Equal(account.Id, session.AccountId);
        Assert.Equal(83, _agencies.LastCode);
        Assert.Equal(_agencies.Agency!.Id, _accounts.LastAgencyId);
        Assert.True(session.IsValid());
        Assert.Same(session, Assert.Single(_sessions.Sessions));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_WithUnknownAccount_RejectsLogin(bool agency)
    {
        var dto = new CreateAuthenticationSessionDto(agency ? 83 : null, agency ? null : "unknown@example.com", "correct-password");
        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCase().ExecuteAsync(dto));
        Assert.Null(_hasher.LastPassword);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownAgency_RejectsLogin()
    {
        _agencies.Agency = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCase().ExecuteAsync(new(83, null, "correct-password")));
        Assert.Null(_accounts.LastAgencyId);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task ExecuteAsync_WithWrongPasswordOrInactiveAccount_RejectsLogin(bool agency, bool active)
    {
        Account account = SetAccount(agency ? Role.Agency : Role.Admin, active);
        string password = active ? "wrong-password" : "correct-password";
        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCase().ExecuteAsync(
            new(agency ? 83 : null, agency ? null : account.Email, password)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ExecuteAsync_WithoutIdentifier_RejectsLogin(string? email) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCase().ExecuteAsync(new(null, email, "correct-password")));

    [Fact]
    public async Task ExecuteAsync_WithBothIdentifiers_RejectsAmbiguousLogin()
    {
        SetAccount(Role.Agency);
        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCase().ExecuteAsync(new(83, "test@example.com", "correct-password")));
    }

    [Fact]
    public async Task ExecuteAsync_WithAdminReturnedForAgency_RejectsWrongRole()
    {
        SetAccount(Role.Admin);
        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCase().ExecuteAsync(new(83, null, "correct-password")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithEmptyPassword_DoesNotVerifyOrPersist(string? password)
    {
        SetAccount(Role.Admin);
        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCase().ExecuteAsync(new(null, "test@example.com", password!)));
        Assert.Null(_hasher.LastPassword);
        Assert.Empty(_sessions.Sessions);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancelledToken_DoesNotPersist()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => UseCase().ExecuteAsync(new(83, null, "correct-password"), source.Token));
        Assert.Empty(_sessions.Sessions);
    }

    [Fact]
    public async Task ExecuteAsync_WithAccountFromOtherAgency_DoesNotPersist()
    {
        _accounts.Account = new Account("Other", null, "stored-hash", true, Role.Agency, Guid.NewGuid());
        _accounts.ReturnAccountRegardlessOfAgency = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCase().ExecuteAsync(new(83, null, "correct-password")));
        Assert.Empty(_sessions.Sessions);
        Assert.Null(_hasher.LastPassword);
    }

    [Fact]
    public async Task ExecuteAsync_UsesInjectedClockAndDuration()
    {
        var clock = new FixedTimeProvider();
        SetAccount(Role.Admin);
        var useCase = new CreateAuthenticationSessionUseCase(_accounts, _agencies, _hasher, _sessions, clock, TimeSpan.FromMinutes(30));
        var session = await useCase.ExecuteAsync(new(null, "test@example.com", "correct-password"));
        Assert.Equal(clock.GetUtcNow().UtcDateTime, session.CreatedAt);
        Assert.Equal(session.CreatedAt.AddMinutes(30), session.ExpiresAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStorageFails_DoesNotReturnSession()
    {
        SetAccount(Role.Admin);
        _sessions.FailOnAdd = true;
        await Assert.ThrowsAsync<IOException>(() => UseCase().ExecuteAsync(new(null, "test@example.com", "correct-password")));
        Assert.Empty(_sessions.Sessions);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRehashIsNeeded_UpdatesHashAndCreatesSession()
    {
        var account = SetAccount(Role.Admin);
        _hasher.RequireRehash = true;
        var session = await UseCase().ExecuteAsync(new(null, account.Email, "correct-password"));
        Assert.Equal("renewed-hash", account.HashedPassword);
        Assert.Equal(1, _accounts.UpdateCalls);
        Assert.Same(session, Assert.Single(_sessions.Sessions));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class SessionsStub : IAuthenticationSessionRepository
    {
        public List<Session> Sessions { get; } = new();
        public bool FailOnAdd { get; set; }
        public Task AddAsync(Session session, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailOnAdd) throw new IOException("Storage unavailable.");
            Sessions.Add(session);
            return Task.CompletedTask;
        }
        public Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Session session, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class HasherStub : IPasswordHasher
    {
        public bool RequireRehash { get; set; }
        public string? LastPassword { get; private set; }
        public string? LastHash { get; private set; }
        public HashVerificationResult VerifyPassword(string password, string hashedPassword)
        {
            LastPassword = password;
            LastHash = hashedPassword;
            return password == "correct-password" && hashedPassword == "stored-hash"
                ? (RequireRehash ? HashVerificationResult.NeedsRehash : HashVerificationResult.Success) : HashVerificationResult.Failed;
        }
        public string HashPassword(string password) => "renewed-hash";
    }

    private sealed class AccountsStub : IAccountRepository
    {
        public int UpdateCalls { get; private set; }
        public Account? Account { get; set; }
        public bool ReturnAccountRegardlessOfAgency { get; set; }
        public string? LastEmail { get; private set; }
        public Guid? LastAgencyId { get; private set; }
        public Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            LastEmail = email;
            return Task.FromResult(Account?.Email == email ? Account : null);
        }
        public Task<Account?> GetByAgencyIdAsync(Guid agencyId, CancellationToken cancellationToken = default)
        {
            LastAgencyId = agencyId;
            return Task.FromResult(ReturnAccountRegardlessOfAgency || Account?.AgencyId == agencyId ? Account : null);
        }
        public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Account>> ListByQueryAsync(ListAccountsQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken = default)
        { UpdateCalls++; return Task.FromResult(account); }
        public Task DeleteAsync(Account account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAgencyAccountAsync(Guid agencyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class AgenciesStub : IAgencyRepository
    {
        public Agency? Agency { get; set; } = new("Épinal", 83);
        public int? LastCode { get; private set; }
        public Task<Agency?> GetByCodeAsync(int code, CancellationToken cancellationToken = default)
        {
            LastCode = code;
            return Task.FromResult(Agency?.Code == code ? Agency : null);
        }
        public Task<Agency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Agency[]?> ListByQueryAsync(ListAgenciesQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Agency> AddAsync(Agency agency, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
