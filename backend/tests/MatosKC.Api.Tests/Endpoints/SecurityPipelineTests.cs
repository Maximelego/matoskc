using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
namespace MatosKC.Api.Tests.Endpoints;
// Tests of the real authentication middleware, requiring neither Docker nor a database.
public sealed class SecurityPipelineTests
{
    private static WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:MatosKCDatabase", "Host=localhost;Database=unused;Username=unused;Password=unused");
        builder.UseSetting("S3:ServiceUrl", "http://unused.test");
        builder.UseSetting("S3:Region", "us-east-1");
        builder.UseSetting("S3:AccessKey", "test");
        builder.UseSetting("S3:SecretKey", "test");
        builder.UseSetting("S3:BucketName", "test");
    });
    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/accounts")]
    [InlineData("/api/agencies")]
    [InlineData("/api/equipments")]
    public async Task ProtectedEndpoints_RequireAuthentication(string path)
    {
        await using var factory = CreateFactory(); using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path, TestContext.Current.CancellationToken)).StatusCode);
    }
    [Fact]
    public async Task CsrfCookie_UsesSecureHttpOnlyHostPrefix()
    {
        await using var factory = CreateFactory(); using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/api/auth/csrf", TestContext.Current.CancellationToken); response.EnsureSuccessStatusCode();
        string cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("__Host-MatosKC.Csrf=", cookie);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("X-CSRF-TOKEN", body.RootElement.GetProperty("headerName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("token").GetString()));
    }
    [Fact]
    public async Task Login_RequiresCsrfBeforeCredentialChecks()
    {
        await using var factory = CreateFactory(); using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new { password = "" }, TestContext.Current.CancellationToken)).StatusCode);
        await MatosKCApiFactory.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { password = "" }, TestContext.Current.CancellationToken)).StatusCode);
    }
    [Fact]
    public async Task Login_IsRateLimited()
    {
        await using var factory = CreateFactory(); using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await MatosKCApiFactory.RefreshCsrfAsync(client);
        for (int i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { password = "" }, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/login", new { password = "" }, TestContext.Current.CancellationToken)).StatusCode);
    }
    [Fact]
    public async Task FabricatedCookie_DoesNotAuthenticate()
    {
        await using var factory = CreateFactory(); using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Cookie", "__Host-MatosKC.Session=forged");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken)).StatusCode);
    }
}
