using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MatosKC.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public sealed class AdministrationEndpointsTests(MatosKCApiFactory factory)
{
    private static async Task<Guid> Id(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task AgencyCrud_AndDuplicateCode()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        int code = Random.Shared.Next(100000, int.MaxValue);
        Guid id = await Id(await client.PostAsJsonAsync(
            "/agencies", new { name = "Création", code }, TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/agencies", new { name = "Doublon", code },
                 TestContext.Current.CancellationToken))
                .StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync($"/agencies/{id}", new { name = "Modifiée", code },
                 TestContext.Current.CancellationToken))
                .StatusCode);
        using (var agency = JsonDocument.Parse(await client.GetStringAsync(
                   $"/agencies/{id}", TestContext.Current.CancellationToken)))
            Assert.Equal("Modifiée", agency.RootElement.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/agencies/{id}", TestContext.Current.CancellationToken))
                .StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/agencies/{id}", TestContext.Current.CancellationToken))
                .StatusCode);
    }

    [Fact]
    public async Task AccountCrud_ProtectsHashAndAgencyReferences()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        Guid agencyId = await Id(await client.PostAsJsonAsync("/agencies",
            new { name = "Agence", code = Random.Shared.Next(100000, int.MaxValue) },
            TestContext.Current.CancellationToken));
        string email = $"account-{Guid.NewGuid():N}@example.com";
        Guid id = await Id(await client.PostAsJsonAsync("/accounts",
            new { displayName = "Utilisateur", email, password = "secret", role = "Admin",
                agencyId },
            TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/agencies/{agencyId}",
                                                   TestContext.Current.CancellationToken))
                                                  .StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync($"/accounts/{id}",
                 new { displayName = "Modifié", email, isActive = false, password = (string?)null },
                 TestContext.Current.CancellationToken))
                .StatusCode);
        string account =
            await client.GetStringAsync($"/accounts/{id}", TestContext.Current.CancellationToken);
        Assert.DoesNotContain("hashedPassword", account, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", account, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/accounts/{id}", TestContext.Current.CancellationToken))
                .StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/accounts/{id}", TestContext.Current.CancellationToken))
                .StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/agencies/{agencyId}",
                                                    TestContext.Current.CancellationToken))
                                                   .StatusCode);
    }

    [Fact]
    public async Task DuplicateEmail_IsCaseInsensitive()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        string email = $"duplicate-{Guid.NewGuid():N}@example.com";
        await Id(await client.PostAsJsonAsync("/accounts",
            new { displayName = "First", email, password = "secret", role = "SuperAdmin" },
            TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/accounts",
                 new { displayName = "Second", email = email.ToUpperInvariant(),
                     password = "secret", role = "SuperAdmin" },
                 TestContext.Current.CancellationToken))
                .StatusCode);
    }

    [Fact]
    public async Task PasswordReset_RevokesSessionsAndPreservesOmittedActiveState()
    {
        using var administrator = await factory.CreateAuthenticatedClientAsync();
        using var user = await factory.CreateAuthenticatedClientAsync(
            MatosKC.Domain.Entities.Accounts.Role.Admin);
        using var me = JsonDocument.Parse(
            await user.GetStringAsync("/auth/me", TestContext.Current.CancellationToken));
        Guid id = me.RootElement.GetProperty("id").GetGuid();
        var response = await administrator.PutAsJsonAsync($"/accounts/{id}",
            new { displayName = "Updated", email = me.RootElement.GetProperty("email").GetString(),
                password = "new-password" },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        using var updated = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(updated.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await user.GetAsync("/auth/me", TestContext.Current.CancellationToken)).StatusCode);
        await MatosKCApiFactory.RefreshCsrfAsync(user);
        Assert.Equal(
            HttpStatusCode.OK, (await user.PostAsJsonAsync("/auth/login",
                                    new { email = me.RootElement.GetProperty("email").GetString(),
                                        password = "new-password" },
                                    TestContext.Current.CancellationToken))
                                   .StatusCode);
    }

    [Fact]
    public async Task OwnAccount_CannotBeDeletedOrDisabled()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        using var me = JsonDocument.Parse(
            await client.GetStringAsync("/auth/me", TestContext.Current.CancellationToken));
        var root = me.RootElement;
        Guid id = root.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.DeleteAsync($"/accounts/{id}", TestContext.Current.CancellationToken))
                .StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PutAsJsonAsync($"/accounts/{id}",
                 new { displayName = "Self", email = root.GetProperty("email").GetString(),
                     isActive = false },
                 TestContext.Current.CancellationToken))
                .StatusCode);
    }
}
