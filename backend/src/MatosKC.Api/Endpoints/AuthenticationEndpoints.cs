using System.Security.Claims;
using MatosKC.Api.Authentication;
using MatosKC.Application.Accounts.Get;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Auth.CreateSession;
using MatosKC.Application.Auth.RevokeSession;
using MatosKC.Domain.Entities.AuthenticationSession;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MatosKC.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static void MapAuthenticationEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/auth")
            .WithTags("Authentication")
            .WithMetadata(new RequireCsrfValidation());

        group.MapGet("/csrf", GetCsrfToken)
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting("login");

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentAccountAsync)
            .RequireAuthorization();
    }

    private static IResult GetCsrfToken(HttpContext context, IAntiforgery antiforgery)
    {
        context.Response.Headers.CacheControl = "no-store";
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);

        return Results.Ok(new
        {
            token = tokens.RequestToken,
            headerName = tokens.HeaderName
        });
    }

    private static async Task<IResult> LoginAsync(
        CreateAuthenticationSessionDto dto,
        CreateAuthenticationSessionUseCase useCase,
        IAccountRepository accounts,
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        context.Response.Headers.CacheControl = "no-store";

        if (dto.Password?.Length > 1024)
        {
            return Results.BadRequest(new { message = "Password is too long." });
        }

        AuthenticationSession session;

        try
        {
            session = await useCase.ExecuteAsync(dto, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Results.Problem(statusCode: 401, title: "Invalid credentials");
        }

        var account = await accounts.GetByIdAsync(session.AccountId, cancellationToken);

        if (account is null || !account.IsActive)
        {
            return Results.Unauthorized();
        }

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            SessionCookieEvents.Principal(account, session.Id),
            new AuthenticationProperties
            {
                IsPersistent = false,
                IssuedUtc = session.CreatedAt,
                ExpiresUtc = session.ExpiresAt
            }
        );

        return Results.Ok(new
        {
            account = AccountResponse.From(account),
            sessionExpiresAt = session.ExpiresAt
        });
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        RevokeAuthenticationSessionUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        string? sessionClaim = context.User.FindFirstValue(SessionCookieEvents.SessionClaim);

        if (Guid.TryParse(sessionClaim, out Guid sessionId))
        {
            await useCase.ExecuteAsync(sessionId, cancellationToken);
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Response.Headers.CacheControl = "no-store";

        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentAccountAsync(
        HttpContext context,
        IAccountRepository accounts,
        CancellationToken cancellationToken
    )
    {
        context.Response.Headers.CacheControl = "no-store";

        Guid accountId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var account = await accounts.GetByIdAsync(accountId, cancellationToken);

        return account is null
            ? Results.Unauthorized()
            : Results.Ok(AccountResponse.From(account));
    }
}
