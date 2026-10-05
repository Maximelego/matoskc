namespace MatosKC.Application.Accounts.Ports;

using MatosKC.Domain.Entities.Accounts;
using MatosKC.Application.Accounts.List;

public interface IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<Account?> ListByQueryAsync(ListAccountsQuery query, CancellationToken cancellationToken = default);

    public Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default);

    public Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken = default);

    public Task DeleteAsync(Account account, CancellationToken cancellationToken = default);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
