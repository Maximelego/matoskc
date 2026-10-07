using MatosKC.Application.Accounts.Create;
using MatosKC.Application.Accounts.List;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Agencies.List;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using Xunit;

namespace MatosKC.Application.Tests.Accounts.Create;

public class CreateAccountUseCaseTests
{
    private readonly AccountRepositoryStub _accounts = new();
    private readonly AgencyRepositoryStub _agencies = new();
    private readonly PasswordHasherStub _hasher = new();

    private CreateAccountUseCase CreateUseCase() => new(_accounts, _agencies, _hasher);

    private CreateAccountDto CreateDto(Role role) => new(
        "Compte test", role == Role.Agency ? null : "maxime@example.com",
        "test-password", role, role == Role.SuperAdmin ? null : _agencies.Agency.Id);

    [Theory]
    [InlineData(Role.SuperAdmin)]
    [InlineData(Role.Admin)]
    [InlineData(Role.Agency)]
    public async Task ExecuteAsync_WithValidAccount_PersistsHashedPasswordAndReturnsId(Role role)
    {
        CreateAccountDto dto = CreateDto(role);

        Guid id = await CreateUseCase().ExecuteAsync(dto);

        Account account = Assert.Single(_accounts.Accounts);
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(account.Id, id);
        Assert.Equal(dto.DisplayName, account.DisplayName);
        Assert.Equal(dto.Email, account.Email);
        Assert.Equal(dto.Role, account.Role);
        Assert.Equal(dto.AgencyId, account.AgencyId);
        Assert.True(account.IsActive);
        Assert.Equal(dto.Password, _hasher.LastPassword);
        Assert.Equal(PasswordHasherStub.Hash, account.HashedPassword);
        Assert.NotEqual(dto.Password, account.HashedPassword);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithEmptyPassword_DoesNotHashOrPersist(string? password)
    {
        CreateAccountDto dto = CreateDto(Role.Admin) with { Password = password! };

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateUseCase().ExecuteAsync(dto));

        Assert.Null(_hasher.LastPassword);
        Assert.Empty(_accounts.Accounts);
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.SuperAdmin)]
    public async Task ExecuteAsync_WithDuplicateEmail_DoesNotPersist(Role role)
    {
        CreateAccountDto dto = CreateDto(role);
        _accounts.Accounts.Add(new Account("Existant", dto.Email, "existing-hash", true, role, dto.AgencyId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateUseCase().ExecuteAsync(dto));

        Assert.Single(_accounts.Accounts);
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.Agency)]
    public async Task ExecuteAsync_WithUnknownAgency_DoesNotPersist(Role role)
    {
        CreateAccountDto dto = CreateDto(role) with { AgencyId = Guid.NewGuid() };

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateUseCase().ExecuteAsync(dto));

        Assert.Empty(_accounts.Accounts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_WithExistingAgencyAccount_DoesNotCreateSecondAccount(bool isActive)
    {
        CreateAccountDto dto = CreateDto(Role.Agency);
        _accounts.Accounts.Add(new Account("Agence existante", null, "existing-hash", isActive, Role.Agency, dto.AgencyId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateUseCase().ExecuteAsync(dto));

        Assert.Single(_accounts.Accounts);
    }

    [Fact]
    public async Task ExecuteAsync_WithAdminInAgency_AllowsSharedAgencyAccount()
    {
        _accounts.Accounts.Add(new Account("Admin", "admin@example.com", "existing-hash", true, Role.Admin, _agencies.Agency.Id));

        await CreateUseCase().ExecuteAsync(CreateDto(Role.Agency));

        Assert.Equal(2, _accounts.Accounts.Count);
        Assert.Single(_accounts.Accounts, account => account.Role == Role.Agency);
    }

    [Fact]
    public async Task ExecuteAsync_WithSharedAccountInOtherAgency_AllowsAgencyAccount()
    {
        _accounts.Accounts.Add(new Account("Autre agence", null, "existing-hash", true, Role.Agency, Guid.NewGuid()));

        await CreateUseCase().ExecuteAsync(CreateDto(Role.Agency));

        Assert.Equal(2, _accounts.Accounts.Count);
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.Agency)]
    public async Task ExecuteAsync_WithMissingAgency_DoesNotPersist(Role role)
    {
        CreateAccountDto dto = CreateDto(role) with { AgencyId = null };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateUseCase().ExecuteAsync(dto));

        Assert.Empty(_accounts.Accounts);
    }

    // Test doubles only: these are not production repository or hashing implementations.
    private sealed class PasswordHasherStub : IPasswordHasher
    {
        public const string Hash = "test-only-hash";
        public string? LastPassword { get; private set; }
        public string HashPassword(string password)
        {
            LastPassword = password;
            return Hash;
        }
        public HashVerificationResult VerifyPassword(string password, string hashedPassword) => throw new NotSupportedException();
    }

    private sealed class AccountRepositoryStub : IAccountRepository
    {
        public List<Account> Accounts { get; } = [];
        public Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            Accounts.Add(account);
            return Task.FromResult(account);
        }
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.Any(account => account.Email == email));
        public Task<bool> ExistsAgencyAccountAsync(Guid agencyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.Any(account => account.Role == Role.Agency && account.AgencyId == agencyId));
        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Account?> GetByAgencyIdAsync(Guid agencyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Account>> ListByQueryAsync(ListAccountsQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Account account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class AgencyRepositoryStub : IAgencyRepository
    {
        public Agency Agency { get; } = new("Épinal", 83);
        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == Agency.Id);
        public Task<Agency?> GetByCodeAsync(int code, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Agency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Agency[]?> ListByQueryAsync(ListAgenciesQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Agency> AddAsync(Agency agency, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

