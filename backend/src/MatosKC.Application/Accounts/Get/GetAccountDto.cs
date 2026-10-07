namespace MatosKC.Application.Accounts.Get;

public sealed record GetAccountDto(
    Guid Id,
    string DisplayName,
    string? Email,
    bool IsActive
);
