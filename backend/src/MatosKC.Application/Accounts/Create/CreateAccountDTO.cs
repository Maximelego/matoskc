namespace MatosKC.Application.Accounts.Create;

using MatosKC.Domain.Entities.Accounts;

public sealed record CreateAccountDto(
    string DisplayName,
    string? Email,
    string Password,
    Role Role,
    Guid? AgencyId
);
