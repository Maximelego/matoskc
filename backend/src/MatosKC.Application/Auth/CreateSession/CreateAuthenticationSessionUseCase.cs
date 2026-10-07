namespace MatosKC.Application.Auth.CreateSession;

using MatosKC.Domain.Entities.AuthenticationSession;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Domain.Entities.Accounts;

public class CreateAuthenticationSessionUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAgencyRepository _agencyRepository;
    private readonly TimeSpan SessionDuration;

    public CreateAuthenticationSessionUseCase(IAccountRepository accountRepository, IAgencyRepository agencyRepository, IPasswordHasher passwordHasher)
    {
        _accountRepository = accountRepository;
        _agencyRepository = agencyRepository;
        _passwordHasher = passwordHasher;

        SessionDuration = new TimeSpan(1, 0, 0); // 1 hour // TODO : Change this to be env vars.
    }

    public async Task<AuthenticationSession> ExecuteAsync(CreateAuthenticationSessionDto dto)
    {
        if (dto.AgencyCode.HasValue)
        {
            return await CreateAgencySession(dto.AgencyCode.Value, dto.Password);
        }
        else if (!string.IsNullOrEmpty(dto.Email))
        {
            return await CreateUserSession(dto.Email, dto.Password);
        }
        else
        {
            throw new InvalidOperationException("Either AgencyCode or Email must be provided.");
        }
    }

    private async Task<AuthenticationSession> CreateAgencySession(int agencyCode, string password)
    {
        // Get the agency by its code
        var agency = await _agencyRepository.GetByCodeAsync(agencyCode) ?? throw new InvalidOperationException("Invalid agency code or password.");
        var agencyAccount = await _accountRepository.GetByAgencyIdAsync(agency.Id) ?? throw new InvalidOperationException("Invalid agency code or password.");

        if (!IsAccountValid(agencyAccount, password))
            throw new InvalidOperationException("Invalid agency code or password.");

        return CreateSessionForAccount(agencyAccount);
    }

    private async Task<AuthenticationSession> CreateUserSession(string email, string password)
    {
        // Get the user account by email
        var account = await _accountRepository.GetByEmailAsync(email) ?? throw new InvalidOperationException("Invalid email or password.");

        if (!IsAccountValid(account, password))
            throw new InvalidOperationException("Invalid email or password.");

        return CreateSessionForAccount(account);
    }

    private bool IsAccountValid(Account account, string password)
    {
        return _passwordHasher.VerifyPassword(password, account.HashedPassword) && account.IsActive;
    }

    private AuthenticationSession CreateSessionForAccount(Account account)
    {
        return new AuthenticationSession(
            account.Id,
            SessionDuration
        );
    }
}
