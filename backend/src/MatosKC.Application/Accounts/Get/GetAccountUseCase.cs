namespace MatosKC.Application.Accounts.Get;

using MatosKC.Application.Accounts.Ports;
using MatosKC.Domain.Entities.Accounts;

public class GetAccountUseCase
{

    private readonly IAccountRepository _accountRepository;

    public GetAccountUseCase(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Account> ExecuteAsync(Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        return account ?? throw new InvalidOperationException($"Account with ID {accountId} not found.");
    }
}
