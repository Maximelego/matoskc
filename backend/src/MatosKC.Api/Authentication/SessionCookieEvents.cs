using System.Security.Claims;
using MatosKC.Application.Auth.VerifySession;
using MatosKC.Domain.Entities.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace MatosKC.Api.Authentication;
public sealed class SessionCookieEvents(VerifyAuthenticationSessionUseCase verify) : CookieAuthenticationEvents
{
    public const string SessionClaim = "session_id";
    public static ClaimsPrincipal Principal(Account account, Guid sessionId) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
        new Claim(ClaimTypes.Name, account.DisplayName),
        new Claim(ClaimTypes.Role, account.Role.ToString()),
        new Claim(SessionClaim, sessionId.ToString()),
        new Claim("agency_id", account.AgencyId?.ToString() ?? "")
    ], CookieAuthenticationDefaults.AuthenticationScheme));
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue(SessionClaim), out Guid id))
        { await RejectAsync(context); return; }
        var account = await verify.ExecuteAsync(id, context.HttpContext.RequestAborted);
        if (account == null) { await RejectAsync(context); return; }
        // Always use the current account data, never stale role claims.
        context.ReplacePrincipal(Principal(account, id));
    }
    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    { context.Response.StatusCode = 401; return Task.CompletedTask; }
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    { context.Response.StatusCode = 403; return Task.CompletedTask; }
}
