namespace MatosKC.Application.Accounts.List;

using MatosKC.Domain.Entities.Accounts;

public sealed record ListAccountsQuery
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public Role? Role { get; set; }
}
