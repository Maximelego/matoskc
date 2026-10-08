using MatosKC.Application.Accounts.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatosKC.Api.Authentication;

public static class BootstrapAdministrator
{
    public static async Task EnsureAsync(
        IServiceProvider services,
        IConfiguration configuration
    )
    {
        string? email = configuration["Authentication:BootstrapAdmin:Email"];
        string? password = configuration["Authentication:BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("BootstrapAdmin requires both Email and Password.");
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();

        bool hasAdministrator = await db.Accounts.AnyAsync(
            account => account.Role == Role.SuperAdmin && account.IsActive
        );

        if (hasAdministrator)
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        string displayName = configuration["Authentication:BootstrapAdmin:DisplayName"]
            ?? "Super administrateur";

        var administrator = new Account(
            displayName,
            email.Trim(),
            hasher.HashPassword(password),
            true,
            Role.SuperAdmin,
            null
        );

        db.Accounts.Add(administrator);
        await db.SaveChangesAsync();
    }
}
