using System.Net;
using System.Threading.RateLimiting;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Auth.CreateSession;
using MatosKC.Application.Auth.Ports;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
namespace MatosKC.Api.Authentication;
public static class AuthenticationSetup
{
    public static IServiceCollection AddSessionAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        double hours = configuration.GetValue<double?>("Authentication:SessionHours") ?? 8;
        if (hours <= 0 || hours > 168) throw new InvalidOperationException("SessionHours must be between 0 and 168.");
        services.AddScoped(sp => new CreateAuthenticationSessionUseCase(
            sp.GetRequiredService<IAccountRepository>(), sp.GetRequiredService<IAgencyRepository>(),
            sp.GetRequiredService<IPasswordHasher>(), sp.GetRequiredService<IAuthenticationSessionRepository>(),
            sp.GetRequiredService<TimeProvider>(), TimeSpan.FromHours(hours)));
        services.AddScoped<SessionCookieEvents>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.Name = "__Host-MatosKC.Session";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(hours);
            options.SlidingExpiration = false;
            options.EventsType = typeof(SessionCookieEvents);
        });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy("ManageAccounts", policy => policy.RequireRole("SuperAdmin"));
            options.AddPolicy("ManageAgencies", policy => policy.RequireRole("SuperAdmin"));
        });
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-MatosKC.Csrf";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        var protection = services.AddDataProtection().SetApplicationName("MatosKC");
        string? keyPath = configuration["Authentication:DataProtectionKeysPath"];
        if (!string.IsNullOrWhiteSpace(keyPath)) protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Only trust explicitly configured proxy IPs, in addition to the framework's loopback defaults.
            foreach (var value in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
                if (!string.IsNullOrWhiteSpace(value)) options.KnownProxies.Add(IPAddress.Parse(value));
        });
        int loginPermitLimit = configuration.GetValue<int?>("Authentication:LoginPermitLimit") ?? 10;
        if (loginPermitLimit <= 0) throw new InvalidOperationException("LoginPermitLimit must be positive.");
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = loginPermitLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }
}
