namespace MatosKC.Application.Auth.CreateSession;

using MatosKC.Domain.Entities.AuthenticationSession;
using MatosKC.Application.Accounts.Ports;

public class CreateAuthenticationSessionUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;

    private readonly TimeSpan SessionDuration;

    public CreateAuthenticationSessionUseCase(IAccountRepository accountRepository, IPasswordHasher passwordHasher)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;

        SessionDuration = new TimeSpan(1, 0, 0); // 1 hour // TODO : Change this to be env vars.
    }

    public async Task<AuthenticationSession> ExecuteAsync(string email, string password)
    {
        var account = await _accountRepository.GetByEmailAsync(email);

        if (account is null || !_passwordHasher.VerifyPassword(password, account.HashedPassword))
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        return new AuthenticationSession(
            account.Id,
            SessionDuration
        );
    }
}
