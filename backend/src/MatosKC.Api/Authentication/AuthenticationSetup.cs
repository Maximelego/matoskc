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

namespace MatosKC.Api.Authentication;

public static class AuthenticationSetup
{
    public static IServiceCollection AddSessionAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        double hours = configuration.GetValue<double?>("Authentication:SessionHours") ?? 8;

        if (hours is <= 0 or > 168)
        {
            throw new InvalidOperationException("SessionHours must be between 0 and 168.");
        }

        TimeSpan sessionDuration = TimeSpan.FromHours(hours);

        services.AddScoped(provider => new CreateAuthenticationSessionUseCase(
            provider.GetRequiredService<IAccountRepository>(),
            provider.GetRequiredService<IAgencyRepository>(),
            provider.GetRequiredService<IPasswordHasher>(),
            provider.GetRequiredService<IAuthenticationSessionRepository>(),
            provider.GetRequiredService<TimeProvider>(),
            sessionDuration
        ));

        services.AddScoped<SessionCookieEvents>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => ConfigureSessionCookie(options, sessionDuration));

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

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

        ConfigureDataProtection(services, configuration);
        ConfigureReverseProxy(services, configuration);
        ConfigureLoginRateLimit(services, configuration);

        return services;
    }

    private static void ConfigureSessionCookie(
        CookieAuthenticationOptions options,
        TimeSpan sessionDuration
    )
    {
        options.Cookie.Name = "__Host-MatosKC.Session";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = sessionDuration;
        options.SlidingExpiration = false;
        options.EventsType = typeof(SessionCookieEvents);
    }

    private static void ConfigureDataProtection(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        IDataProtectionBuilder protection = services.AddDataProtection()
            .SetApplicationName("MatosKC");

        string? keyPath = configuration["Authentication:DataProtectionKeysPath"];

        if (!string.IsNullOrWhiteSpace(keyPath))
        {
            protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        }
    }

    private static void ConfigureReverseProxy(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        string[] knownProxies = configuration.GetSection("ReverseProxy:KnownProxies")
            .Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto;

            // Keep the framework's loopback defaults and trust only configured proxies.
            foreach (string address in knownProxies)
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    options.KnownProxies.Add(IPAddress.Parse(address));
                }
            }
        });
    }

    private static void ConfigureLoginRateLimit(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        int permitLimit = configuration.GetValue<int?>("Authentication:LoginPermitLimit") ?? 10;

        if (permitLimit <= 0)
        {
            throw new InvalidOperationException("LoginPermitLimit must be positive.");
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }
            ));
        });
    }
}
