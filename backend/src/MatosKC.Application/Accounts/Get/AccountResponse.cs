using MatosKC.Domain.Entities.Accounts;
namespace MatosKC.Application.Accounts.Get;
public sealed record AccountResponse(Guid Id, string DisplayName, string? Email, Role Role, Guid? AgencyId, bool IsActive)
{
    public static AccountResponse From(Account account) => new(account.Id, account.DisplayName, account.Email, account.Role, account.AgencyId, account.IsActive);
}
