using System.Security.Claims;
using MatosKC.Api.Authentication;
using MatosKC.Application.Accounts.Get;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Auth.CreateSession;
using MatosKC.Application.Auth.RevokeSession;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace MatosKC.Api.Endpoints;
public static class AuthenticationEndpoints
{
    public static void MapAuthenticationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");
        group.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken, headerName = tokens.HeaderName });
        }).AllowAnonymous();
        group.MapPost("/login", async (CreateAuthenticationSessionDto dto, CreateAuthenticationSessionUseCase useCase,
            IAccountRepository accounts, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (dto.Password?.Length > 1024) return Results.BadRequest(new { message = "Password is too long." });
            MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession session;
            try { session = await useCase.ExecuteAsync(dto, ct); }
            catch (InvalidOperationException) { return Results.Problem(statusCode: 401, title: "Invalid credentials"); }
            var account = await accounts.GetByIdAsync(session.AccountId, ct);
            if (account == null || !account.IsActive) return Results.Unauthorized();
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                SessionCookieEvents.Principal(account, session.Id),
                new AuthenticationProperties { IsPersistent = false, IssuedUtc = session.CreatedAt, ExpiresUtc = session.ExpiresAt });
            return Results.Ok(new { account = AccountResponse.From(account), sessionExpiresAt = session.ExpiresAt });
        }).AllowAnonymous().RequireRateLimiting("login");
        group.MapPost("/logout", async (HttpContext context, RevokeAuthenticationSessionUseCase useCase, CancellationToken ct) =>
        {
            if (Guid.TryParse(context.User.FindFirstValue(SessionCookieEvents.SessionClaim), out Guid id))
                await useCase.ExecuteAsync(id, ct);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Headers.CacheControl = "no-store";
            return Results.NoContent();
        }).RequireAuthorization();
        group.MapGet("/me", async (HttpContext context, IAccountRepository accounts, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var account = await accounts.GetByIdAsync(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
            return account == null ? Results.Unauthorized() : Results.Ok(AccountResponse.From(account));
        }).RequireAuthorization();
    }
}
