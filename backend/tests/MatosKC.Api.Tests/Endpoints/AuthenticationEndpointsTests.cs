using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace MatosKC.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public sealed class AuthenticationEndpointsTests(MatosKCApiFactory factory)
{
    private HttpClient Anonymous() => factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    [Fact]
    public async Task AnonymousMe_Returns401()
    { using var client = Anonymous(); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken)).StatusCode); }
    [Fact]
    public async Task InvalidCredentials_Return401()
    {
        using var client = Anonymous(); await MatosKCApiFactory.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = "missing@example.com", password = "incorrect" }, TestContext.Current.CancellationToken)).StatusCode);
    }
    [Fact]
    public async Task LoginWithoutCsrf_Returns400()
    {
        using var client = Anonymous();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new { email = "missing@example.com", password = "incorrect" }, TestContext.Current.CancellationToken)).StatusCode);
    }
    [Theory]
    [InlineData(Role.SuperAdmin)]
    [InlineData(Role.Admin)]
    [InlineData(Role.Agency)]
    public async Task LoginAndMe_ContainPublicAccountOnly(Role role)
    {
        using var client = await factory.CreateAuthenticatedClientAsync(role);
        var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken); response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(role.ToString(), json.RootElement.GetProperty("role").GetString());
        Assert.DoesNotContain("hashedPassword", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }
    [Fact]
    public async Task Logout_RevokesSessionAndClearsIdentity()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken)).StatusCode);
    }
    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.Agency)]
    public async Task NonSuperAdmin_CannotManageAccountsOrAgencies(Role role)
    {
        using var client = await factory.CreateAuthenticatedClientAsync(role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/accounts", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/agencies", new { name = "Denied", code = 9000 }, TestContext.Current.CancellationToken)).StatusCode);
    }
    [Fact]
    public async Task RevokedServerSession_RejectsExistingCookie()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        using var me = JsonDocument.Parse(await client.GetStringAsync("/api/auth/me", TestContext.Current.CancellationToken));
        Guid accountId = me.RootElement.GetProperty("id").GetGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
            var session = await db.AuthenticationSessions.SingleAsync(x => x.AccountId == accountId, TestContext.Current.CancellationToken);
            session.Revoke(); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken)).StatusCode);
    }
    [Fact]
    public async Task AccountDeactivation_RejectsExistingCookie()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        using var me = JsonDocument.Parse(await client.GetStringAsync("/api/auth/me", TestContext.Current.CancellationToken));
        Guid accountId = me.RootElement.GetProperty("id").GetGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
            var account = await db.Accounts.SingleAsync(x => x.Id == accountId, TestContext.Current.CancellationToken);
            account.DeactivateAccount(); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken)).StatusCode);
    }
    [Fact]
    public async Task CurrentRole_IsReloadedFromDatabase()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(Role.Admin);
        using var me = JsonDocument.Parse(await client.GetStringAsync("/api/auth/me", TestContext.Current.CancellationToken));
        Guid accountId = me.RootElement.GetProperty("id").GetGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
            await db.Accounts.Where(x => x.Id == accountId).ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Role, Role.SuperAdmin).SetProperty(x => x.AgencyId, (Guid?)null), TestContext.Current.CancellationToken);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/accounts", TestContext.Current.CancellationToken)).StatusCode);
    }
    [Fact]
    public async Task AuthenticationCookie_IsSecureAndCannotBeReplayedAfterLogout()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        using var me = JsonDocument.Parse(await client.GetStringAsync("/api/auth/me", TestContext.Current.CancellationToken));
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = me.RootElement.GetProperty("email").GetString(),
            password = "integration-test-password"
        }, TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();
        string cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith("__Host-MatosKC.Session="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        await MatosKCApiFactory.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken)).StatusCode);
        using var replay = Anonymous(); replay.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/auth/me", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task MutationWithoutCsrf_IsRejected()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(); client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/agencies", new { name = "Denied", code = 9001 }, TestContext.Current.CancellationToken)).StatusCode);
    }
}
