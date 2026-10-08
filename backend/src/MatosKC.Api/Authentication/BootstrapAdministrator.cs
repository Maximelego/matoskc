using MatosKC.Application.Accounts.Ports;
using MatosKC.Infrastructure.Persistence;
namespace MatosKC.Api.Authentication;

public static class BootstrapAdministrator
{
    public static async Task EnsureAsync(IServiceProvider services, IConfiguration configuration)
    {
        string? email = configuration["Authentication:BootstrapAdmin:Email"];
        string? password = configuration["Authentication:BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("BootstrapAdmin requires both Email and Password.");
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
        if (await db.Accounts.AnyAsync(x => x.Role == Role.SuperAdmin && x.IsActive)) return;
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Accounts.Add(new Account(configuration["Authentication:BootstrapAdmin:DisplayName"] ?? "Super administrateur",
            email.Trim(), hasher.HashPassword(password), true, Role.SuperAdmin, null));
        await db.SaveChangesAsync();
    }
}
