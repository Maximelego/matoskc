namespace MatosKC.Application.Accounts.List;

using MatosKC.Application.Accounts.Ports;

public class ListAccountsUseCase
{
    private readonly IAccountRepository _accountRepository;

    public ListAccountsUseCase(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<IReadOnlyList<MatosKC.Application.Accounts.Get.AccountResponse>> ExecuteAsync(ListAccountsQuery query, CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.ListByQueryAsync(query, cancellationToken);
        return accounts.Select(MatosKC.Application.Accounts.Get.AccountResponse.From).ToArray();
    }
}
