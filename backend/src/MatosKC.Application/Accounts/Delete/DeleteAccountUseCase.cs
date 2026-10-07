namespace MatosKC.Application.Accounts.Delete;

using MatosKC.Application.Accounts.Ports;

public class DeleteAccountUseCase
{
    private readonly IAccountRepository _accountRepository;

    public DeleteAccountUseCase(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task ExecuteAsync(Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);

        if (account is null)
        {
            throw new InvalidOperationException($"Account with ID {accountId} not found.");
        }

        await _accountRepository.DeleteAsync(account);
    }
}
