namespace MatosKC.Application.Accounts.List;

using MatosKC.Application.Accounts.Ports;

public class ListAccountsUseCase
{
    private readonly IAccountRepository _accountRepository;

    public ListAccountsUseCase(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task ExecuteAsync(ListAccountsQuery query, CancellationToken cancellationToken = default)
    {
        // TODO: Implement the logic to list accounts based on the provided query parameters.
        throw new NotImplementedException("ListAccountsUseCase is not implemented yet.");
    }
}
