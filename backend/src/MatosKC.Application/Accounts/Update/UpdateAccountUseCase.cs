using MatosKC.Application.Accounts.Get;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Administration;
namespace MatosKC.Application.Accounts.Update;
// Role and agency remain immutable, as in Account.
public sealed record UpdateAccountDto(string DisplayName, string? Email, bool? IsActive = null, string? Password = null);
public sealed class UpdateAccountUseCase(IAccountRepository accounts, IPasswordHasher hasher,
    IAdministrationRepository administration, TimeProvider clock)
{
    public async Task<AccountResponse> ExecuteAsync(Guid id, UpdateAccountDto dto, CancellationToken ct = default)
    {
        var account = await accounts.GetByIdAsync(id, ct) ?? throw new AdministrationException(404, "Account not found.");
        if (dto.Email != null)
        {
            var other = await accounts.GetByEmailAsync(dto.Email, ct);
            if (other != null && other.Id != id) throw new AdministrationException(409, "Email already exists.");
        }
        if (dto.Password != null && string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Password cannot be empty.");
        // Validate all inputs before changing the tracked entity.
        _ = new MatosKC.Domain.Entities.Accounts.Account(id, dto.DisplayName, dto.Email,
            account.HashedPassword, dto.IsActive ?? account.IsActive, account.Role, account.AgencyId);
        account.UpdateDisplayName(dto.DisplayName);
        account.UpdateEmail(dto.Email);
        if (dto.IsActive == true) account.ActivateAccount();
        else if (dto.IsActive == false) account.DeactivateAccount();
        if (dto.Password != null) account.UpdateHashedPassword(hasher.HashPassword(dto.Password));
        // Staged revocations are committed atomically by UpdateAsync.
        if (dto.IsActive == false || dto.Password != null)
            await administration.RevokeAccountSessionsAsync(id, clock.GetUtcNow().UtcDateTime, ct);
        await accounts.UpdateAsync(account, ct);
        return AccountResponse.From(account);
    }
}
